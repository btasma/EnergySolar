# EnergySolarLogger

A simple .NET Core Systemd service that collects energy usage and sends it to an InfluxDb database.

1. It can receive data from an Arduino that's connected to the the P1 port of the Smart Energy meter from Liander

2. It monitors the current energy production of a SolarMax solar panel installation (every 30 seconds)


## Configuration

1. Publish a build for the desired platform and copy the files
1. Copy energysolarlogger.service to the /etc/systemd/system folder
2. Open the service file in a text editor
3. Change the WorkingDirectory and ExecStart directory to the folder
4. Change the environment vars to the IP address of the SolarMax device and the InfluxDb database:

Environment="INFLUX_ADDRESS=http://localhost:8086"
Environment="INFLUX_DB=energysolar"
Environment="SOLARMAX_IP=192.168.1.5"
Environment="SOLARMAX_PORT=12345"

5. Save the file
6. systemctl start energysolarlogger

## Receive data

Send a GET http request in the following format:

http://1.2.3.4:5005?u=totalEnergyUsed&p=totalEnergyProvided&cp=currentEnergyProvided&cu=currentEnergyUsed
http://1.2.3.4:5005?u=5000&p=1200&cp=10&cu=20
