package com.example.esp32rgbcontroller

import android.content.Context
import android.content.Intent
import android.net.ConnectivityManager
import android.net.LinkProperties
import android.net.NetworkCapabilities
import android.net.wifi.WifiManager
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.widget.ArrayAdapter
import android.widget.Button
import android.widget.ListView
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.InetAddress
import java.net.URL
import java.util.concurrent.ConcurrentLinkedQueue
import java.util.concurrent.Executors
import java.util.concurrent.TimeUnit

class MainActivity : AppCompatActivity() {
    private lateinit var statusText: TextView
    private lateinit var scanButton: Button
    private lateinit var deviceList: ListView
    private lateinit var addButton: Button

    private val devices = mutableListOf<DiscoveredDevice>()
    private lateinit var adapter: ArrayAdapter<String>
    private val mainHandler = Handler(Looper.getMainLooper())

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        statusText = findViewById(R.id.statusText)
        scanButton = findViewById(R.id.scanButton)
        deviceList = findViewById(R.id.deviceList)
        addButton = findViewById(R.id.addButton)

        adapter = ArrayAdapter(this, android.R.layout.simple_list_item_1, mutableListOf())
        deviceList.adapter = adapter

        scanButton.setOnClickListener {
            startScan()
        }

        deviceList.setOnItemClickListener { _, _, position, _ ->
            val selected = devices[position]
            addButton.isEnabled = true
            addButton.tag = selected
        }

        addButton.setOnClickListener {
            val selected = addButton.tag as? DiscoveredDevice ?: return@setOnClickListener
            openControl(selected.baseUrl)
        }
    }

    private fun openControl(baseUrl: String) {
        val intent = Intent(this, ControlActivity::class.java)
        intent.putExtra(ControlActivity.EXTRA_BASE_URL, baseUrl)
        startActivity(intent)
    }

    private fun startScan() {
        statusText.text = "Suche im Netzwerk..."
        devices.clear()
        adapter.clear()
        addButton.isEnabled = false
        addButton.tag = null

        val networkInfo = resolveWifiNetwork()
        if (networkInfo == null) {
            statusText.text = "Keine WLAN-Verbindung gefunden"
            return
        }

        val network = networkInfo.address and networkInfo.mask
        val broadcast = network or networkInfo.mask.inv()
        val addresses = ConcurrentLinkedQueue<Int>()
        for (addr in network + 1 until broadcast) {
            addresses.add(addr)
        }

        val executor = Executors.newFixedThreadPool(20)
        val total = addresses.size
        var scanned = 0

        repeat(20) {
            executor.execute {
                while (true) {
                    val current = addresses.poll() ?: break
                    val ipString = intToIp(current)
                    val baseUrl = "http://$ipString:8080"
                    if (probeDevice(baseUrl)) {
                        val name = fetchDeviceName(baseUrl)
                        addDevice(DiscoveredDevice(baseUrl, name))
                    }
                    scanned++
                    if (scanned % 50 == 0) {
                        updateStatus("Scan: $scanned/$total")
                    }
                }
            }
        }

        executor.shutdown()
        Thread {
            executor.awaitTermination(60, TimeUnit.SECONDS)
            updateStatus("Scan abgeschlossen: ${devices.size} Geräte")
        }.start()
    }

    private fun probeDevice(baseUrl: String): Boolean {
        return try {
            val url = URL("$baseUrl/api/state")
            val conn = url.openConnection() as HttpURLConnection
            conn.connectTimeout = 600
            conn.readTimeout = 600
            conn.inputStream.use { it.readBytes() }
            conn.responseCode == 200
        } catch (_: Exception) {
            false
        }
    }

    private fun fetchDeviceName(baseUrl: String): String {
        return try {
            val url = URL("$baseUrl/api/state")
            val conn = url.openConnection() as HttpURLConnection
            conn.connectTimeout = 800
            conn.readTimeout = 800
            val payload = conn.inputStream.bufferedReader().readText()
            val json = JSONObject(payload)
            json.optString("deviceName", "ESP32")
        } catch (_: Exception) {
            "ESP32"
        }
    }

    private fun addDevice(device: DiscoveredDevice) {
        mainHandler.post {
            if (devices.none { it.baseUrl == device.baseUrl }) {
                devices.add(device)
                adapter.add("${device.name} (${device.baseUrl})")
                adapter.notifyDataSetChanged()
            }
        }
    }

    private fun updateStatus(text: String) {
        mainHandler.post {
            statusText.text = text
        }
    }

    private fun intToIp(value: Int): String {
        return InetAddress.getByAddress(
            byteArrayOf(
                (value and 0xFF).toByte(),
                (value shr 8 and 0xFF).toByte(),
                (value shr 16 and 0xFF).toByte(),
                (value shr 24 and 0xFF).toByte()
            )
        ).hostAddress ?: "0.0.0.0"
    }

    private fun resolveWifiNetwork(): NetworkInfo? {
        val connectivity = getSystemService(Context.CONNECTIVITY_SERVICE) as ConnectivityManager
        val active = connectivity.activeNetwork ?: return resolveWifiNetworkLegacy()
        val caps = connectivity.getNetworkCapabilities(active) ?: return resolveWifiNetworkLegacy()
        if (!caps.hasTransport(NetworkCapabilities.TRANSPORT_WIFI)) {
            return null
        }
        val link = connectivity.getLinkProperties(active) ?: return resolveWifiNetworkLegacy()
        val info = resolveIpv4(link)
        return info ?: resolveWifiNetworkLegacy()
    }

    private fun resolveIpv4(link: LinkProperties): NetworkInfo? {
        for (address in link.linkAddresses) {
            val inet = address.address
            if (inet is java.net.Inet4Address) {
                val ipInt = inet.address
                val ip = (ipInt[0].toInt() and 0xFF) or
                    ((ipInt[1].toInt() and 0xFF) shl 8) or
                    ((ipInt[2].toInt() and 0xFF) shl 16) or
                    ((ipInt[3].toInt() and 0xFF) shl 24)
                val prefix = address.prefixLength
                val mask = if (prefix == 0) 0 else (-1 shl (32 - prefix))
                return NetworkInfo(ip, mask)
            }
        }
        return null
    }

    private fun resolveWifiNetworkLegacy(): NetworkInfo? {
        val wifiManager = applicationContext.getSystemService(Context.WIFI_SERVICE) as WifiManager
        val dhcp = wifiManager.dhcpInfo ?: return null
        val ip = dhcp.ipAddress
        val mask = dhcp.netmask
        if (ip == 0 || mask == 0) {
            return null
        }
        return NetworkInfo(ip, mask)
    }

    private data class NetworkInfo(val address: Int, val mask: Int)

    private data class DiscoveredDevice(val baseUrl: String, val name: String)
}
