#!/bin/bash
echo "🔧 Building for Production..."
dotnet publish -c Release -o ./publish-prod

echo "🚀 Deploying to Azure (prod)..."
pulumi stack select provision41/prod
pulumi config set environment prod
pulumi up --yes
