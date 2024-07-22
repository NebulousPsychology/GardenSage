# Recorder

## All-in-one Service

```mermaid
graph TB

subgraph device
    schedule
    subgraph recordersvc
        GetState(/swagger)
        RecordState( /facility_state)
        RecordTemperature
        GetPredicted
    end
    sensors
end

inquire(/gpio_info)
state(manual)
ExternalForecast
    
schedule-->schedule
inquire --> GetState
schedule-->RecordTemperature
RecordTemperature<-->sensors[[sensors]]
schedule-->GetPredicted
GetPredicted <--> ExternalForecast 
state --manual report state change--> RecordState
RecordState -->RecordTemperature
```

<https://learn.microsoft.com/en-us/archive/msdn-magazine/2005/march/scheduling-asp-net-code-to-run-using-web-and-windows-services>

## homeassistant device

```
bluetoothctl

list
show

devices
scan.
pair

```

- client wlan, can be positioned anywhere

```mermaid
graph LR

openmeteo
subgraph wlan/eth
    subgraph "HAOS device  @ 192.168.0.[A]"
        usb[[con:usb]]
        eth0[[con:eth0]]
        wlanb[[con:wlan-broadcast]]
        bluetooth[[con:bluetooth]]
        subgraph hacontainer
            ha_timetrigger
            ha_webhook
        end
    end
    subgraph "recordersvc @ 192.168.0.[B]"
        RecordTemperature(/set_temperature) --> datastore
        GetPredicted(/forecast)
        RecordState(/facility_state)
        GetState(/swagger)
    end


subgraph "automation net"
    thermometer1 & thermometer2 --> hub(brand-x hub hardware)
    thermometer3 & thermometer4 .-> bluetooth
    wifidevice("wifi device[s]")
end
end
inquire(/gpio_info) 

wifidevice -.- wlanb -- ?? can read ?? --> hacontainer
hub -.- usb -- /dev/usb* --> hacontainer
bluetooth --> hacontainer

inquire --> GetState 
ha_timetrigger -- restcommand GET --> GetPredicted
ha_timetrigger -- restcommand POST --> RecordTemperature

RecordState -->RecordTemperature
GetPredicted --> ha_webhook
eth0 --> Internet --> openmeteo --> GetPredicted
```

## homeassistant containerized

- wired internet ()
- broadcast AP for smarthome-only traffic from devices

```mermaid
graph TB

openmeteo

subgraph device
    cronjob -- curl --> ha_webhook 
    cronjob --> ha_timetrigger
    usb[[con:usb]]
    eth0[[con:eth0]]
    wlanb[[con:wlan-broadcast]]
    bluetooth[[con:bluetooth]]
    subgraph "recordersvc @ host.docker.internal"
        RecordTemperature(/set_temperature)
        GetPredicted(/forecast)
        RecordState(/facility_state)
        GetState(/swagger)
    end
    subgraph hacontainer
        ha_timetrigger
        ha_webhook
    end
    RecordTemperature --> datastore
end

subgraph "automation net"
    thermometer1 & thermometer2 --> hub(brand-x hub hardware)
    thermometer3 & thermometer4 .-> bluetooth
    wifidevice("wifi device[s]")
end
inquire(/gpio_info) 

wifidevice -.- wlanb -- ?? can read ?? --> hacontainer
hub -.- usb -- /dev/usb* --> hacontainer
bluetooth --> hacontainer

inquire --> GetState 
ha_timetrigger -- restcommand GET --> GetPredicted
ha_timetrigger -- restcommand POST --> RecordTemperature

RecordState -->RecordTemperature
GetPredicted --> ha_webhook
GetPredicted --> eth0 --> Internet --> openmeteo --> GetPredicted
```
