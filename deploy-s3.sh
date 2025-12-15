#!/bin/bash
# S3 Frontend Deployment Script für Rechnungsfreigabe

# Variablen - ÄNDERN SIE DIESE WERTE
BUCKET_NAME="rechnungsfreigabe-frontend"  # Eindeutiger Bucket-Name
AWS_REGION="eu-central-1"                # AWS Region
DIST_FOLDER="./dist/frontend"           # Build-Ausgabe-Ordner

echo "🚀 Frontend Deployment nach S3 Bucket: $BUCKET_NAME"

# 1. Prüfen ob AWS CLI konfiguriert ist
echo "📋 AWS CLI Konfiguration prüfen..."
if ! aws sts get-caller-identity > /dev/null 2>&1; then
    echo "❌ AWS CLI ist nicht konfiguriert. Bitte führen Sie 'aws configure' aus."
    exit 1
fi

# 2. S3 Bucket erstellen (falls nicht vorhanden)
echo "📦 S3 Bucket erstellen..."
aws s3 mb s3://$BUCKET_NAME --region $AWS_REGION 2>/dev/null || echo "Bucket existiert bereits oder Fehler beim Erstellen"

# 3. Bucket für Website-Hosting konfigurieren
echo "🌐 Website-Hosting aktivieren..."
aws s3 website s3://$BUCKET_NAME --index-document index.html --error-document index.html

# 4. Bucket-Policy für öffentlichen Zugriff setzen
echo "🔓 Öffentliche Leserechte konfigurieren..."
cat > bucket-policy.json << EOF
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
EOF

aws s3api put-bucket-policy --bucket $BUCKET_NAME --policy file://bucket-policy.json

# 5. Öffentlichen Zugriff entsperren
echo "🔐 Bucket öffentlichen Zugriff aktivieren..."
aws s3api put-public-access-block --bucket $BUCKET_NAME --public-access-block-configuration "BlockPublicAcls=false,IgnorePublicAcls=false,BlockPublicPolicy=false,RestrictPublicBuckets=false"

# 6. Frontend-Dateien hochladen
echo "📤 Frontend-Dateien hochladen..."
if [ -d "$DIST_FOLDER" ]; then
    aws s3 sync $DIST_FOLDER s3://$BUCKET_NAME --delete \
        --cache-control "public, max-age=31536000" \
        --exclude "index.html" \
        --exclude "*.map"
    
    # index.html ohne Cache hochladen
    aws s3 cp $DIST_FOLDER/index.html s3://$BUCKET_NAME/index.html \
        --cache-control "public, max-age=0, must-revalidate" \
        --content-type "text/html"
    
    echo "✅ Upload erfolgreich!"
    echo ""
    echo "🌐 Ihre Website ist verfügbar unter:"
    echo "   http://$BUCKET_NAME.s3-website.$AWS_REGION.amazonaws.com"
    echo ""
    echo "📝 Notizen:"
    echo "   - Erste Bereitstellung kann einige Minuten dauern"
    echo "   - Für HTTPS verwenden Sie CloudFront"
    echo "   - API-URL in environment.prod.ts prüfen"
else
    echo "❌ Dist-Ordner nicht gefunden: $DIST_FOLDER"
    echo "   Führen Sie zuerst 'npm run build' aus"
    exit 1
fi

# 7. CloudFront Distribution erstellen (optional)
read -p "🚀 Möchten Sie eine CloudFront Distribution erstellen? (y/N): " create_cloudfront
if [[ $create_cloudfront =~ ^[Yy]$ ]]; then
    echo "☁️ CloudFront Distribution wird erstellt..."
    
    cat > cloudfront-config.json << EOF
{
    "CallerReference": "rechnungsfreigabe-$(date +%s)",
    "Comment": "Rechnungsfreigabe Frontend Distribution",
    "DefaultCacheBehavior": {
        "TargetOriginId": "S3-$BUCKET_NAME",
        "ViewerProtocolPolicy": "redirect-to-https",
        "MinTTL": 0,
        "ForwardedValues": {
            "QueryString": false,
            "Cookies": {
                "Forward": "none"
            }
        }
    },
    "Origins": {
        "Quantity": 1,
        "Items": [
            {
                "Id": "S3-$BUCKET_NAME",
                "DomainName": "$BUCKET_NAME.s3-website.$AWS_REGION.amazonaws.com",
                "CustomOriginConfig": {
                    "HTTPPort": 80,
                    "HTTPSPort": 443,
                    "OriginProtocolPolicy": "http-only"
                }
            }
        ]
    },
    "Enabled": true,
    "CustomErrorResponses": {
        "Quantity": 1,
        "Items": [
            {
                "ErrorCode": 404,
                "ResponsePagePath": "/index.html",
                "ResponseCode": "200"
            }
        ]
    }
}
EOF

    DISTRIBUTION_ID=$(aws cloudfront create-distribution --distribution-config file://cloudfront-config.json --query 'Distribution.Id' --output text)
    DOMAIN_NAME=$(aws cloudfront get-distribution --id $DISTRIBUTION_ID --query 'Distribution.DomainName' --output text)
    
    echo "✅ CloudFront Distribution erstellt!"
    echo "   Distribution ID: $DISTRIBUTION_ID"
    echo "   HTTPS URL: https://$DOMAIN_NAME"
    echo "   ⏳ Bereitstellung dauert 5-15 Minuten"
fi

# Cleanup
rm -f bucket-policy.json cloudfront-config.json

echo ""
echo "🎉 Deployment abgeschlossen!"