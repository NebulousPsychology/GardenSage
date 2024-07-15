# leverage builtin container support?
# https://devblogs.microsoft.com/dotnet/announcing-builtin-container-support-for-the-dotnet-sdk/
dotnet publish -p:PublishProfile=DefaultContainer # creates 'gardensage-recorder:latest'
docker-compose up
# how to bundle a docker image and deploy to remote?
# docker save -o recorder.tar gardensage-recorder:latest
# scp ...
# onremote: docker load -i

# on wsl:
# wsl -e docker save  gardensage-recorder:latest | gzip | ssh user@host docker load

# docker run -P -e ASPNETCORE_HTTP_PORTS=12345 gardensage-recorder
# docker run -p 192.168.0.106:8089:12345 -e ASPNETCORE_HTTP_PORTS=12345 gardensage-recorder # No
#option 1 modify the etc/hosts file
# ...
#option 2 expose, not just bind
# docker run -p 8089:12345 --expose 8089 -e ASPNETCORE_HTTP_PORTS=12345 gardensage-recorder
# docker run -p 8089:12345 --network=host -e ASPNETCORE_HTTP_PORTS=12345 gardensage-recorder # NO
# docker run --privileged -p 192.168.0.2:8089:12345 -e ASPNETCORE_HTTP_PORTS=12345 gardensage-recorder
# https://docs.docker.com/network/

# https://tecadmin.net/forwarding-ports-to-docker-containers-using-linux-firewalls/