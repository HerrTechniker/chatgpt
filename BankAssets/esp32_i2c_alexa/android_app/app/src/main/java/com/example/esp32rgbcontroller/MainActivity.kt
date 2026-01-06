package com.example.esp32rgbcontroller

import android.os.Bundle
import android.webkit.WebSettings
import android.webkit.WebView
import android.webkit.WebViewClient
import android.widget.Button
import android.widget.EditText
import androidx.appcompat.app.AppCompatActivity

class MainActivity : AppCompatActivity() {
    private lateinit var urlInput: EditText
    private lateinit var openButton: Button
    private lateinit var webView: WebView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        urlInput = findViewById(R.id.urlInput)
        openButton = findViewById(R.id.openButton)
        webView = findViewById(R.id.webView)

        webView.webViewClient = WebViewClient()
        val settings: WebSettings = webView.settings
        settings.javaScriptEnabled = true
        settings.domStorageEnabled = true

        openButton.setOnClickListener {
            val url = urlInput.text.toString().trim().ifEmpty { "http://esp32.local:8080" }
            webView.loadUrl(url)
        }

        webView.loadUrl("http://esp32.local:8080")
    }
}
