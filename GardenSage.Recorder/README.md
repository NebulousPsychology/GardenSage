# Recorder

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

https://learn.microsoft.com/en-us/archive/msdn-magazine/2005/march/scheduling-asp-net-code-to-run-using-web-and-windows-services
