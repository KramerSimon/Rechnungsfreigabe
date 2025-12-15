# PowerShell Upload Script für den bestehenden "rechnungsfreigabe" Bucket

$BUCKET_NAME = "rechnungsfreigabe"
$AWS_REGION = "eu-central-1"
$DIST_FOLDER = "./frontend/dist/frontend"

Write-Host "🚀 Upload zu Bucket: $BUCKET_NAME" -ForegroundColor Green

# Prüfen ob die Build-Dateien existieren
if (!(Test-Path $DIST_FOLDER)) {
    Write-Host "❌ Frontend noch nicht gebaut. Führen Sie zuerst aus:" -ForegroundColor Red
    Write-Host "   cd frontend" -ForegroundColor Yellow
    Write-Host "   npm run build" -ForegroundColor Yellow
    exit 1
}

# Prüfen ob AWS CLI verfügbar ist
try {
    aws --version | Out-Null
    Write-Host "✅ AWS CLI gefunden" -ForegroundColor Green
} catch {
    Write-Host "❌ AWS CLI nicht installiert." -ForegroundColor Red
    Write-Host "📥 Download: https://awscli.amazonaws.com/AWSCLIV2.msi" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "🌐 Alternative: Verwenden Sie die AWS Management Console" -ForegroundColor Cyan
    Write-Host "   1. Öffnen Sie https://console.aws.amazon.com/s3" -ForegroundColor White
    Write-Host "   2. Klicken Sie auf Bucket '$BUCKET_NAME'" -ForegroundColor White
    Write-Host "   3. Upload alle Dateien aus: $DIST_FOLDER" -ForegroundColor White
    exit 1
}

# AWS Konfiguration prüfen
try {
    aws sts get-caller-identity | Out-Null
    Write-Host "✅ AWS Konfiguration OK" -ForegroundColor Green
} catch {
    Write-Host "⚙️ AWS CLI konfigurieren:" -ForegroundColor Yellow
    Write-Host "   aws configure" -ForegroundColor White
    exit 1
}

# Website-Hosting aktivieren
Write-Host "🌐 Website-Hosting aktivieren..." -ForegroundColor Yellow
aws s3 website "s3://$BUCKET_NAME" --index-document index.html --error-document index.html

# Öffentlichen Zugriff konfigurieren
Write-Host "🔓 Öffentliche Zugriffsrechte..." -ForegroundColor Yellow
aws s3api put-public-access-block --bucket $BUCKET_NAME --public-access-block-configuration "BlockPublicAcls=false,IgnorePublicAcls=false,BlockPublicPolicy=false,RestrictPublicBuckets=false"

# Bucket Policy
$policy = @"
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

$policy | Out-File -FilePath "policy.json" -Encoding utf8
aws s3api put-bucket-policy --bucket $BUCKET_NAME --policy file://policy.json

# Dateien hochladen
Write-Host "📤 Frontend hochladen..." -ForegroundColor Yellow
aws s3 sync $DIST_FOLDER "s3://$BUCKET_NAME" --delete --cache-control "public, max-age=31536000" --exclude "index.html"
aws s3 cp "$DIST_FOLDER/index.html" "s3://$BUCKET_NAME/index.html" --cache-control "public, max-age=0, must-revalidate" --content-type "text/html"

# Cleanup
Remove-Item "policy.json" -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "🎉 Upload erfolgreich!" -ForegroundColor Green
Write-Host "🌐 URL: http://$BUCKET_NAME.s3-website.$AWS_REGION.amazonaws.com" -ForegroundColor Cyan