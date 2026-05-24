FROM ubuntu:22.04 AS build

RUN apt-get update -y && \
    apt-get install -y wget && \
    wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb

RUN apt-get update -y && \
    apt-get install -y dotnet-sdk-8.0 openjdk-21-jdk build-essential libcap2-bin

WORKDIR /app
COPY . .

RUN dotnet publish -c Release -o OUT

EXPOSE 3636
ENTRYPOINT [ "dotnet", "OUT/Juiz.dll" ]