# ScriptRunnerPOC


## How to install

Cross-platform recommended approach


```shell
dotnet tool install --global ScriptRunnerGUI --no-cache --ignore-failed-sources
```


## Generate action definition from `PowerShell` script

```pwsh
function Get-ActionDefinition
{
  param($CommandPath)
  
  $data = Get-Command $CommandPath
  
   [Ordered]@{
    name = [regex]::Replace($data.Name.TrimEnd(".ps1"), "(\p{Ll})(\p{Lu})", '$1 $2')
    command = "pwsh -NoProfile -Command "+ $(Resolve-Path $CommandPath -Relative)
    autoParameterBuilderStyle = "powershell"
    params = foreach($key in  $data.Parameters.Keys)
    {
      [Ordered]@{
        name = $key
        description  = [regex]::Replace($key, "(\p{Ll})(\p{Lu})", '$1 $2')
        prompt = if($data.Parameters[$key].SwitchParameter)
                {
                  "checkbox" 
                }
                elseif($data.Parameters[$key].ParameterType.Name -eq "DateTime")
                {
                  "datePicker"
                }
                elseif($key -match "path")
                {
                  "filePicker"
                }
                else{
                  "text"
                }
      }
    }
  } | ConvertTo-Json -Depth 10
}
```
