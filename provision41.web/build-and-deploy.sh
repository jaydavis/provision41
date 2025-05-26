#!/bin/bash

set -e

# Validate input
if [ -z "$1" ]; then
  echo "❌ Usage: ./build-and-deploy.sh [dev|prod]"
  exit 1
fi

ENV="$1"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Configs per environment
if [ "$ENV" == "dev" ]; then
  STACK="provision41/provision41/dev"
  RESOURCE_GROUP="provision41-dev-rg"
  WEBAPP_NAME="provision41-dev-webapp"
elif [ "$ENV" == "prod" ]; then
  STACK="provision41/provision41/prod"
  RESOURCE_GROUP="provision41-prod-rg"
  WEBAPP_NAME="provision41-prod-webapp"
else
  echo "❌ Invalid environment: $ENV (use 'dev' or 'prod')"
  exit 1
fi

# Pulumi (optional)
if [ -d "$SCRIPT_DIR/../pulumi" ]; then
  echo "🔄 Running Pulumi..."
  cd "$SCRIPT_DIR/../pulumi"
  pulumi up --stack "$STACK" --yes
else
  echo "ℹ️ Pulumi directory not found. Skipping infrastructure step."
fi

echo "🔧 Building for $ENV..."

PUBLISH_DIR="$SCRIPT_DIR/publish-$ENV"
ZIP_FILE="$SCRIPT_DIR/publish-$ENV.zip"

# Clean previous publish output
rm -rf "$PUBLISH_DIR"
mkdir -p "$PUBLISH_DIR"

# Build
dotnet publish "$SCRIPT_DIR/provision41.web.csproj" -c Release -o "$PUBLISH_DIR"

# Zip
echo "📦 Creating zip file..."
cd "$PUBLISH_DIR"
[ -f "$ZIP_FILE" ] && rm "$ZIP_FILE"
zip -r "$ZIP_FILE" .
cd "$SCRIPT_DIR"

# Deploy
echo "🚀 Deploying to Azure ($ENV)..."
az webapp deploy \
  --resource-group "$RESOURCE_GROUP" \
  --name "$WEBAPP_NAME" \
  --src-path "$ZIP_FILE" \
  --type zip

echo "✅ Deployment to $ENV complete!"
