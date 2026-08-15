FROM ubuntu:22.04

ARG DEBIAN_FRONTEND=noninteractive
ARG SERVER_BUILD_PATH=Builds/EdgegapServer

COPY ${SERVER_BUILD_PATH} /root/build/

WORKDIR /root/

RUN chmod +x /root/build/ServerBuild

RUN apt-get update && \
    apt-get install -y ca-certificates && \
    apt-get clean && \
    update-ca-certificates

EXPOSE 7777/udp

CMD ["/bin/sh", "-c", "exec /root/build/ServerBuild -batchmode -nographics -logFile - $UNITY_COMMANDLINE_ARGS"]
