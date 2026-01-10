package com.example.esp32rgbcontroller

import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.widget.ArrayAdapter
import android.widget.Button
import android.widget.SeekBar
import android.widget.Spinner
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import org.json.JSONArray
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URL
import java.util.concurrent.Executors

class ControlActivity : AppCompatActivity() {
    private lateinit var deviceNameText: TextView
    private lateinit var nodeSpinner: Spinner
    private lateinit var seekR: SeekBar
    private lateinit var seekG: SeekBar
    private lateinit var seekB: SeekBar
    private lateinit var applyColorButton: Button
    private lateinit var offButton: Button
    private lateinit var effectSpinner: Spinner
    private lateinit var applyEffectButton: Button
    private lateinit var statusText: TextView

    private val mainHandler = Handler(Looper.getMainLooper())
    private val executor = Executors.newSingleThreadExecutor()
    private var baseUrl: String = ""
    private var availableNodes: List<Int> = emptyList()
    private var refreshRunnable: Runnable? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_control)

        baseUrl = intent.getStringExtra(EXTRA_BASE_URL) ?: "http://esp32.local:8080"

        deviceNameText = findViewById(R.id.deviceNameText)
        nodeSpinner = findViewById(R.id.nodeSpinner)
        seekR = findViewById(R.id.seekR)
        seekG = findViewById(R.id.seekG)
        seekB = findViewById(R.id.seekB)
        applyColorButton = findViewById(R.id.applyColorButton)
        offButton = findViewById(R.id.offButton)
        effectSpinner = findViewById(R.id.effectSpinner)
        applyEffectButton = findViewById(R.id.applyEffectButton)
        statusText = findViewById(R.id.statusText)

        effectSpinner.adapter = ArrayAdapter(this, android.R.layout.simple_spinner_item, listOf("solid", "flicker", "rainbow", "pulse")).apply {
            setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
        }

        applyColorButton.setOnClickListener { sendColor(true) }
        offButton.setOnClickListener { sendColor(false) }
        applyEffectButton.setOnClickListener { sendEffect() }

        refreshRunnable = Runnable {
            loadState()
            mainHandler.postDelayed(refreshRunnable!!, 3000)
        }
    }

    override fun onResume() {
        super.onResume()
        loadState()
        refreshRunnable?.let { mainHandler.postDelayed(it, 3000) }
    }

    override fun onPause() {
        super.onPause()
        refreshRunnable?.let { mainHandler.removeCallbacks(it) }
    }

    private fun loadState() {
        executor.execute {
            try {
                val payload = requestJson("$baseUrl/api/state")
                val json = JSONObject(payload)
                val name = json.optString("deviceName", "ESP32")
                val effect = json.optString("effect", "solid")
                val available = json.optJSONArray("available") ?: JSONArray()
                val nodes = json.optJSONArray("nodes") ?: JSONArray()

                availableNodes = buildAvailableList(available)
                val nodeLabels = availableNodes.map { "LED ${it + 1}" }

                mainHandler.post {
                    deviceNameText.text = name
                    nodeSpinner.adapter = ArrayAdapter(this, android.R.layout.simple_spinner_item, nodeLabels).apply {
                        setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
                    }
                    val effectIndex = (0 until effectSpinner.count).firstOrNull { effectSpinner.getItemAtPosition(it) == effect } ?: 0
                    effectSpinner.setSelection(effectIndex)
                    statusText.text = "Status: OK"
                }

                if (availableNodes.isNotEmpty()) {
                    val firstIndex = availableNodes.first()
                    val node = nodes.optJSONObject(firstIndex)
                    if (node != null) {
                        mainHandler.post {
                            seekR.progress = node.optInt("r", 0)
                            seekG.progress = node.optInt("g", 0)
                            seekB.progress = node.optInt("b", 0)
                        }
                    }
                }
            } catch (ex: Exception) {
                mainHandler.post { statusText.text = "Status: ${ex.message}" }
            }
        }
    }

    private fun sendColor(turnOn: Boolean) {
        val idx = nodeSpinner.selectedItemPosition
        if (idx < 0 || idx >= availableNodes.size) {
            return
        }
        val node = availableNodes[idx]
        val r = seekR.progress
        val g = seekG.progress
        val b = seekB.progress
        val on = if (turnOn) 1 else 0
        executor.execute {
            try {
                requestJson("$baseUrl/api/node?node=$node&r=$r&g=$g&b=$b&on=$on", true)
                mainHandler.post { statusText.text = "Status: Farbe gesendet" }
            } catch (ex: Exception) {
                mainHandler.post { statusText.text = "Status: ${ex.message}" }
            }
        }
    }

    private fun sendEffect() {
        val effect = effectSpinner.selectedItem?.toString() ?: "solid"
        executor.execute {
            try {
                requestJson("$baseUrl/api/effect?effect=$effect", true)
                mainHandler.post { statusText.text = "Status: Effekt gesetzt" }
            } catch (ex: Exception) {
                mainHandler.post { statusText.text = "Status: ${ex.message}" }
            }
        }
    }

    private fun buildAvailableList(array: JSONArray): List<Int> {
        val result = mutableListOf<Int>()
        for (i in 0 until array.length()) {
            if (array.optBoolean(i, false)) {
                result.add(i)
            }
        }
        return result
    }

    private fun requestJson(endpoint: String, post: Boolean = false): String {
        val url = URL(endpoint)
        val conn = url.openConnection() as HttpURLConnection
        conn.connectTimeout = 1500
        conn.readTimeout = 1500
        if (post) {
            conn.requestMethod = "POST"
        }
        conn.inputStream.use { return it.bufferedReader().readText() }
    }

    companion object {
        const val EXTRA_BASE_URL = "BASE_URL"
    }
}
