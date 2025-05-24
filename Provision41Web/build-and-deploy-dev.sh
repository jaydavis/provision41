#!/bin/bash
echo "🔧 Building for Development..."
dotnet publish -c Release -o ./publish-dev

echo "🚀 Deploying to Azure (dev)..."
pulumi stack select provision41/dev
pulumi config set environment dev
pulumi up --yes
