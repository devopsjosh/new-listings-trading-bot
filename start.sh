#!/bin/bash

# Step 1: Wait for the database to be ready
# echo "Waiting for the database to be ready..."
# until dotnet ef database update; do
#     >&2 echo "Database is unavailable - sleeping"
#     sleep 1
# done

# Step 2: Check if migration exists
echo "Checking for existing migrations..."
if dotnet ef migrations list | grep -q "createDB"; then
    echo "Migration 'createDB' already exists. Skipping migration step."
else
    # Step 3: Run migrations
    echo "Running migrations..."
    dotnet ef migrations add createDB
    
    # Check if migration was successful
    if [ $? -ne 0 ]; then
        echo "Migration failed."
        exit 1
    else
        echo "Migration 'createDB' added successfully."
    fi
fi

# Step 4: Start the .NET app
echo "Starting the .NET app..."
dotnet watch run --no-launch-profile --urls http://+:5000;https://+:5001