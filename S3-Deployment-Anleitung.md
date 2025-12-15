# 🚀 Frontend auf S3 Bucket hochladen - Anleitung

## 📋 Voraussetzungen

1. **AWS CLI installieren**
   ```bash
   # Windows: https://aws.amazon.com/cli/
   # Download und installieren Sie AWS CLI v2
   ```

2. **AWS CLI konfigurieren**
   ```bash
   aws configure
   # AWS Access Key ID: [Ihr Access Key]
   # AWS Secret Access Key: [Ihr Secret Key]
   # Default region name: eu-central-1
   # Default output format: json
   ```

## 🔨 Frontend für Produktion bauen

```bash
cd frontend
npm install
npm run build
```

## 📦 S3 Bucket erstellen und konfigurieren

### Option 1: Automatisch mit PowerShell Script
```powershell
# Führen Sie das bereitgestellte Script aus:
PowerShell -ExecutionPolicy Bypass -File deploy-s3.ps1
```

### Option 2: Manuell mit AWS CLI

1. **Bucket erstellen**
   ```bash
   aws s3 mb s3://rechnungsfreigabe-frontend-unique --region eu-central-1
   ```

2. **Website-Hosting aktivieren**
   ```bash
   aws s3 website s3://rechnungsfreigabe-frontend-unique --index-document index.html --error-document index.html
   ```

3. **Öffentlichen Zugriff aktivieren**
   ```bash
   # Bucket-Policy erstellen
   echo '{
       "Version": "2012-10-17",
       "Statement": [{
           "Sid": "PublicReadGetObject",
           "Effect": "Allow",
           "Principal": "*",
           "Action": "s3:GetObject",
           "Resource": "arn:aws:s3:::rechnungsfreigabe-frontend-unique/*"
       }]
   }' > bucket-policy.json
   
   aws s3api put-bucket-policy --bucket rechnungsfreigabe-frontend-unique --policy file://bucket-policy.json
   aws s3api put-public-access-block --bucket rechnungsfreigabe-frontend-unique --public-access-block-configuration "BlockPublicAcls=false,IgnorePublicAcls=false,BlockPublicPolicy=false,RestrictPublicBuckets=false"
   ```

4. **Dateien hochladen**
   ```bash
   cd frontend
   
   # Alle Dateien außer index.html mit Cache
   aws s3 sync dist/frontend s3://rechnungsfreigabe-frontend-unique --delete --cache-control "public, max-age=31536000" --exclude "index.html"
   
   # index.html ohne Cache für Updates
   aws s3 cp dist/frontend/index.html s3://rechnungsfreigabe-frontend-unique/index.html --cache-control "public, max-age=0, must-revalidate" --content-type "text/html"
   ```

## 🌐 Website-URL

Nach dem Upload ist Ihre Website verfügbar unter:
```
http://rechnungsfreigabe-frontend-unique.s3-website.eu-central-1.amazonaws.com
```

## 🔒 HTTPS mit CloudFront (Empfohlen)

Für HTTPS-Unterstützung erstellen Sie eine CloudFront Distribution:

1. **AWS Console öffnen** → CloudFront
2. **Create Distribution** → Web
3. **Origin Domain**: rechnungsfreigabe-frontend-unique.s3-website.eu-central-1.amazonaws.com
4. **Viewer Protocol Policy**: Redirect HTTP to HTTPS
5. **Custom Error Pages**: 404 → /index.html (200) für SPA-Routing

## ⚠️ Wichtige Hinweise

- **Bucket-Name muss global eindeutig sein** - fügen Sie Datum/Zufallszeichen hinzu
- **API-URL prüfen**: Die Frontend-App muss auf Ihre EC2-API zeigen
- **CORS konfigurieren**: Stellen Sie sicher, dass Ihre API CORS für die S3-Domain erlaubt
- **Kosten**: S3 + CloudFront kosten ca. 1-5€/Monat für kleine Apps

## 🔄 Updates deployen

Für zukünftige Updates:
```bash
cd frontend
npm run build
aws s3 sync dist/frontend s3://ihr-bucket-name --delete --exclude "*.map"
```

## 🛠️ Troubleshooting

1. **403 Forbidden**: Bucket-Policy und öffentliche Zugriffseinstellungen prüfen
2. **404 für Subroutes**: CloudFront Custom Error Pages konfigurieren
3. **CORS Errors**: API CORS-Konfiguration für S3-Domain erweitern