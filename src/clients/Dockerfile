# Stage 1
FROM ubuntu:latest AS build-env

RUN apt-get update
RUN apt-get install -y curl git wget unzip libgconf-2-4 gdb libstdc++6 libglu1-mesa fonts-droid-fallback lib32stdc++6 python3
RUN apt-get clean

RUN git clone https://github.com/flutter/flutter.git /usr/local/flutter

ENV PATH="/usr/local/flutter/bin:/usr/local/flutter/bin/cache/dart-sdk/bin:${PATH}"

RUN flutter doctor -v

RUN flutter channel master
RUN flutter upgrade
RUN flutter config --enable-web

RUN mkdir /app/
COPY . /app/
WORKDIR /app/
RUN flutter build web
# Stage 2

FROM nginx
COPY --from=build-env /app/build/web /usr/share/nginx/html

COPY /scripts/replace-env.sh /replace-env.sh
RUN chmod +x /replace-env.sh

ENTRYPOINT ["/replace-env.sh"]
CMD ["nginx", "-g", "daemon off;"]