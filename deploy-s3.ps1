# PowerShell Script für S3 Frontend Deployment
# Rechnungsfreigabe Frontend auf AWS S3

# Variablen - ÄNDERN SIE DIESE WERTE
$BUCKET_NAME = "rechnungsfreigabe-frontend-$(Get-Date -Format 'yyyyMMdd')"  # Eindeutiger Bucket-Name
$AWS_REGION = "eu-central-1"                # AWS Region
$DIST_FOLDER = "./dist/frontend"           # Build-Ausgabe-Ordner

Write-Host "🚀 Frontend Deployment nach S3 Bucket: $BUCKET_NAME" -ForegroundColor Green

# 1. Prüfen ob AWS CLI installiert ist
Write-Host "📋 AWS CLI prüfen..." -ForegroundColor Yellow
try {
    aws --version | Out-Null
    Write-Host "✅ AWS CLI gefunden" -ForegroundColor Green
} catch {
    Write-Host "❌ AWS CLI nicht gefunden. Bitte installieren Sie es von: https://aws.amazon.com/cli/" -ForegroundColor Red
    exit 1
}

# 2. AWS Konfiguration prüfen
Write-Host "🔑 AWS Konfiguration prüfen..." -ForegroundColor Yellow
try {
    aws sts get-caller-identity | Out-Null
    Write-Host "✅ AWS CLI ist konfiguriert" -ForegroundColor Green
} catch {
    Write-Host "❌ AWS CLI ist nicht konfiguriert. Führen Sie 'aws configure' aus." -ForegroundColor Red
    exit 1
}

# 3. Frontend bauen
Write-Host "🔨 Frontend bauen..." -ForegroundColor Yellow
if (Test-Path "package.json") {
    npm run build
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build erfolgreich" -ForegroundColor Green
    } else {
        Write-Host "❌ Build fehlgeschlagen" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "❌ package.json nicht gefunden. Wechseln Sie in das frontend-Verzeichnis." -ForegroundColor Red
    exit 1
}

# 4. S3 Bucket erstellen
Write-Host "📦 S3 Bucket erstellen..." -ForegroundColor Yellow
aws s3 mb "s3://$BUCKET_NAME" --region $AWS_REGION 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Bucket erstellt: $BUCKET_NAME" -ForegroundColor Green
} else {
    Write-Host "⚠️ Bucket existiert bereits oder Fehler beim Erstellen" -ForegroundColor Yellow
}

# 5. Website-Hosting aktivieren
Write-Host "🌐 Website-Hosting aktivieren..." -ForegroundColor Yellow
aws s3 website "s3://$BUCKET_NAME" --index-document index.html --error-document index.html

# 6. Bucket-Policy für öffentlichen Zugriff
Write-Host "🔓 Öffentliche Leserechte konfigurieren..." -ForegroundColor Yellow
$bucketPolicy = @"
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "PublicReadGetObject",
            "Effect": "Allow",
            "Principal": "*",
            "Action": "s3:GetObject",
            "Resource": "arn:aws:s3:::$BUCKET_NAME/*"
        }
    ]
}
"@

$bucketPolicy | Out-File -FilePath "bucket-policy.json" -Encoding utf8
aws s3api put-bucket-policy --bucket $BUCKET_NAME --policy file://bucket-policy.json

# 7. Öffentlichen Zugriff aktivieren
Write-Host "🔐 Bucket öffentlichen Zugriff aktivieren..." -ForegroundColor Yellow
aws s3api put-public-access-block --bucket $BUCKET_NAME --public-access-block-configuration "BlockPublicAcls=false,IgnorePublicAcls=false,BlockPublicPolicy=false,RestrictPublicBuckets=false"

# 8. Frontend-Dateien hochladen
Write-Host "📤 Frontend-Dateien hochladen..." -ForegroundColor Yellow
if (Test-Path $DIST_FOLDER) {
    # Alle Dateien außer index.html mit Cache hochladen
    aws s3 sync $DIST_FOLDER "s3://$BUCKET_NAME" --delete --cache-control "public, max-age=31536000" --exclude "index.html" --exclude "*.map"
    
    # index.html ohne Cache hochladen
    aws s3 cp "$DIST_FOLDER/index.html" "s3://$BUCKET_NAME/index.html" --cache-control "public, max-age=0, must-revalidate" --content-type "text/html"
    
    Write-Host "✅ Upload erfolgreich!" -ForegroundColor Green
    Write-Host ""
    Write-Host "🌐 Ihre Website ist verfügbar unter:" -ForegroundColor Cyan
    Write-Host "   http://$BUCKET_NAME.s3-website.$AWS_REGION.amazonaws.com" -ForegroundColor White
    Write-Host ""
    Write-Host "📝 Wichtige Hinweise:" -ForegroundColor Yellow
    Write-Host "   - Erste Bereitstellung kann einige Minuten dauern"
    Write-Host "   - Für HTTPS verwenden Sie CloudFront"
    Write-Host "   - API-URL: http://ec2-3-79-210-32.eu-central-1.compute.amazonaws.com:5000"
    
} else {
    Write-Host "❌ Dist-Ordner nicht gefunden: $DIST_FOLDER" -ForegroundColor Red
    Write-Host "   Stellen Sie sicher, dass der Build erfolgreich war." -ForegroundColor Yellow
    exit 1
}

# Cleanup
Remove-Item "bucket-policy.json" -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "🎉 Deployment abgeschlossen!" -ForegroundColor Green