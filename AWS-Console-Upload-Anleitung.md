# 🌐 Frontend über AWS Management Console hochladen

## 📋 Schritt-für-Schritt Anleitung

### 1. **Bucket für Website-Hosting konfigurieren**
1. Klicken Sie auf Ihren Bucket "rechnungsfreigabe"
2. Gehen Sie zu **Properties** (Eigenschaften)
3. Scrollen Sie zu **Static website hosting**
4. Klicken Sie **Edit**
5. Wählen Sie **Enable**
6. **Index document**: `index.html`
7. **Error document**: `index.html` (für Angular SPA-Routing)
8. Klicken Sie **Save changes**

### 2. **Öffentlichen Zugriff aktivieren**
1. Gehen Sie zu **Permissions** (Berechtigungen)
2. **Block public access (bucket settings)** → Edit
3. **Deaktivieren Sie alle Checkboxen** (alle auf off)
4. Bestätigen Sie mit "confirm"

### 3. **Bucket Policy hinzufügen**
1. Bleiben Sie in **Permissions**
2. Scrollen Sie zu **Bucket policy** → Edit
3. Fügen Sie diese Policy ein:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "PublicReadGetObject",
            "Effect": "Allow",
            "Principal": "*",
            "Action": "s3:GetObject",
            "Resource": "arn:aws:s3:::rechnungsfreigabe/*"
        }
    ]
}
```

### 4. **Frontend-Dateien hochladen**
1. Gehen Sie zu **Objects** (Objekte)
2. Klicken Sie **Upload** (Hochladen)
3. **Add files** → Wählen Sie ALLE Dateien aus diesem Ordner:
   `C:\Users\threk\Documents\Rechnungsfreigabe\frontend\dist\frontend\browser\`
   
   **Genau diese Dateien auswählen:**
   - ✅ `index.html`
   - ✅ `main-KWQYJB2F.js`
   - ✅ `chunk-H4AMSTTL.js`
   - ✅ `chunk-XDQICLZY.js` 
   - ✅ `polyfills-B6TNHZQ6.js`
   - ✅ `styles-36AW6TKX.css`
   - ✅ `favicon.ico`
   - ✅ `images.jpeg`

4. **WICHTIG**: Laden Sie alle Dateien DIREKT in den Bucket-Root hoch (nicht in einen Unterordner)
5. Klicken Sie **Upload**

### 5. **Website-URL**
Nach dem Upload ist Ihre Website verfügbar unter:
```
http://rechnungsfreigabe.s3-website.eu-central-1.amazonaws.com
```

## ⚡ Wichtige Dateien zum Hochladen:
- `index.html` (Haupt-HTML-Datei)
- `main-*.js` (Angular-Anwendung)
- `polyfills-*.js` (Browser-Kompatibilität)
- `styles-*.css` (Stylesheet)
- Alle anderen JavaScript/CSS-Dateien aus dem dist-Ordner