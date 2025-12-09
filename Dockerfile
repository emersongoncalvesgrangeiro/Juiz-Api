FROM ubuntu:latest AS build

RUN apt-get update -y
RUN apt-get update -y
RUN apt-get install dotnet-sdk-8.0 -y
RUN apt-get install openjdk-21-jdk -y
RUN apt-get install build-essential -y
RUN apt-get install libcap2-bin -y

WORKDIR /app

COPY . .

RUN dotnet publish -c Release -o OUT

RUN setcap 'cap_net_bind_service=+ep' /usr/lib/dotnet/dotnet

#FROM docker:rc-dind-rootless

EXPOSE 3636

ENTRYPOINT [ "dotnet", "OUT/Juiz-API.dll" ]