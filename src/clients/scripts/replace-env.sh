#!/bin/bash

envPath='usr/share/nginx/html/assets' 
echo $envPath
if [ -n "$TARGET_ENV" ]; then
  mv /$envPath/env/$TARGET_ENV.env /$envPath/.env
fi

exec "$@"