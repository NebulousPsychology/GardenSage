#requires -Modules pswritehtml
# function Carrier {
#     param (
#         [double] $t,
#     [double]$wavelength,
#     [double]$amp = 1.0,
#     [double]$intercept = 0,
#     [double]$phaseOffset = 0,
#     [bool]$amp_peak2peak = $true
#     )
#     $amp * ($amp_peak2peak ? 0.5 : 1) * ( -[Math]::Cos([Math]::PI*2* ($t - $phaseOffset)/ $wavelength)) + $intercept;
# }
# $hours = 0..11|% { $mo = $_; 0..(24*3)|%{$_+($mo*30*24)} }
# $dat = $hours |%{
#     [PSCustomObject]@{
#         T = $_
#         # C = [math]::Round( (Carrier -t $_ -wavelength (24) -amp 20), 4)
#         # C2 = [math]::Round( (Carrier -t $_ -wavelength (24*2) -amp 10),4)
#         daily=[math]::Round( (Carrier -t $_ -wavelength (24) -amp 26),4)
#         Yearly = [math]::Round( (Carrier -t $_ -wavelength (365*24) -amp 23. -intercept 33),4)
#     } |select T,daily,yearly, @{Name='combo'; Expression={[int]( $_.Yearly+2*$_.daily)}}
# }

# $dat|Format-Table
$global:dat=$null
$dat = import-csv "$PSScriptRoot\GardenSage.Test\bin\Debug\net8.0\yeardata.all.csv" -Header 'day','hour','temp' |%{
    [PSCustomObject]@{
        Day= $_.day
        Hour = $_.Hour
        Temp = [math]::Round( $_.Temp, 4)
        # C2 = [math]::Round( (Carrier -t $_ -wavelength (24*2) -amp 10),4)
        # daily=[math]::Round( (Carrier -t $_ -wavelength (24) -amp 26),4)
        # Yearly = [math]::Round( (Carrier -t $_ -wavelength (365*24) -amp 23. -intercept 33),4)
    }
}
if (-not $dat){ throw "DIE!" }
Write-Host "$($dat.Count) lines"
# $dat|Format-List
$f = New-TemporaryFile
$f = Rename-Item $f -NewName "$($f.Name).html" -Verbose -PassThru
$settings=@{
    TitleText= 'Charts - TimeLine'
    Online=$true
    ShowHTML=$true
    FilePath= $f
}
New-HTML @settings {
    #New-HTMLTabStyle -SlimTabs
    #New-HTMLTab -Name 'TimeLine Charts' -IconRegular hourglass {
    New-HTMLSection -Invisible {
        New-HTMLPanel {
            New-HTMLChart -Title 'carrier waves' -TitleAlignment center {
                New-ChartDataLabel -Enabled:$false
                New-ChartAxisY -Show -TitleText 'degrees' -MinValue -11 -MaxValue 100
                # New-ChartAxisX -Name ($dat.Day ) -Type datetime
                New-ChartAxisX -Name ($dat|%{ ([datetime]'1-1-24').AddHours($_.Hour) } ) -Type datetime
                # New-ChartAxisX -Name $dat.Hour -Type numeric
                New-ChartLine -Name 'T_y' -Value ($dat|select -exp Temp) -Color Green
                New-ChartLine -Name 'T_hot' -Value ($dat|%{78}) -Color Red -Dash 2
                New-ChartLine -Name 'T_cold' -Value ($dat|%{65}) -Color AirForceBlue -Dash 2
            }
        } 
    }
    #}
}
Start-Sleep -Seconds 5
$settings.FilePath|Remove-Item -ErrorAction Continue -Verbose