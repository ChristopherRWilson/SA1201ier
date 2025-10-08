#!/bin/bash
# Clean Visual Studio and build caches
echo "Cleaning Visual Studio and build caches..."

# Remove Visual Studio cache
if [ -d ".vs" ]; then
    rm -rf .vs
    echo "Removed .vs folder"
fi

# Remove obj and bin folders from all projects
find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null
find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null
echo "Removed obj and bin folders"

# Restore packages
echo "Restoring NuGet packages..."
dotnet restore

echo ""
echo "Done! You can now open the solution in Visual Studio."
