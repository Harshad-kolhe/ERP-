$order = @('Suppliers','Customers','Employees','Roles','BusinessUnits','Assemblies','ParentParts','HsnCodes','LookupValues','UnitsOfMeasure','Lookups','Parts')
$root = 'd:\ERP\ERP-\backend\src\Erp.Api\Features'
$found = Get-ChildItem $root -Filter *.cs -Recurse | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } | ForEach-Object {
  $c = Get-Content $_.FullName -Raw
  if($c -match '(?m)^\s*public\s+sealed\s+class\s+(\w+)\s*:\s*IEndpoint'){
    $ns = ([regex]::Match($c,'namespace\s+([\w\.]+);')).Groups[1].Value
    [PSCustomObject]@{ Feature=(($ns -replace '^Erp\.Api\.Features\.?','') -split '\.')[0]; Namespace=$ns; Class=$matches[1] }
  }
}
$target = Join-Path $root 'ErpEndpoints.cs'
if(-not $found -or $found.Count -eq 0){
  if(Test-Path $target){ Remove-Item -LiteralPath $target -Force }
  Write-Output "0 minimal-API endpoints remain - ErpEndpoints.cs deleted"
  return
}
$sb = New-Object System.Text.StringBuilder
foreach($n in ($found.Namespace | Sort-Object -Unique)){ [void]$sb.AppendLine("using $n;") }
[void]$sb.AppendLine("using Microsoft.AspNetCore.Http;")
[void]$sb.AppendLine()
[void]$sb.AppendLine("namespace Erp.Api.Features;")
[void]$sb.AppendLine()
[void]$sb.AppendLine("public static class ErpEndpoints")
[void]$sb.AppendLine("{")
[void]$sb.AppendLine("    public static IEndpointRouteBuilder MapMasters(this IEndpointRouteBuilder endpoints)")
[void]$sb.AppendLine("    {")
[void]$sb.AppendLine("        ArgumentNullException.ThrowIfNull(endpoints);")
[void]$sb.AppendLine()
[void]$sb.AppendLine("        var masters = endpoints")
[void]$sb.AppendLine("            .MapGroup(`"/api/v1/masters`")")
[void]$sb.AppendLine("            .WithMetadata(new TagsAttribute(`"Masters`"));")
[void]$sb.AppendLine()
foreach($f in $order){
  $items = $found | Where-Object { $_.Feature -eq $f } | Sort-Object Class
  if(-not $items){ continue }
  foreach($i in $items){ [void]$sb.AppendLine("        new $($i.Class)().Map(masters);") }
  [void]$sb.AppendLine()
}
[void]$sb.AppendLine("        return endpoints;")
[void]$sb.AppendLine("    }")
[void]$sb.AppendLine("}")
Set-Content $target $sb.ToString() -Encoding utf8
Write-Output "regenerated: $($found.Count) minimal-API endpoints remain"
