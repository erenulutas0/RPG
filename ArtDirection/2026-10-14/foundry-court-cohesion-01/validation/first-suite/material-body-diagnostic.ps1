$ErrorActionPreference='Stop'
$repo='E:/MobileGame'
$bin=Join-Path $repo 'Tools/UnityCompileCheck/bin/Debug/netstandard2.1'
foreach($name in @('UnityEngine.SharedInternalsModule.dll','UnityEngine.CoreModule.dll','UnityEngine.dll','nunit.framework.dll')){
    [Reflection.Assembly]::LoadFrom((Join-Path $bin $name)) | Out-Null
}
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'UnityCompileCheck.dll'))
$test=$assembly.GetType('Cryptforge.Tests.FoundryPavingLayoutTests')
$view=$assembly.GetType('Cryptforge.UI.FoundryRegionView')
$static=[Reflection.BindingFlags]'Static,NonPublic'
$members=[Reflection.BindingFlags]'Instance,Public,NonPublic'
$rows=@()
foreach($origin in @([UnityEngine.Vector2]::new(-30,8),[UnityEngine.Vector2]::new(-28,6),
    [UnityEngine.Vector2]::new(-23,-10),[UnityEngine.Vector2]::new(-15,-12),
    [UnityEngine.Vector2]::new(-8,-6),[UnityEngine.Vector2]::new(1,2),[UnityEngine.Vector2]::new(7,8))){
    $flag=$test.GetMethod('BuildFlag',$static).Invoke($null,@($origin.x,$origin.y,[single]3,[single]2,0))
    $uv=$flag.GetType().GetField('U',$members).GetValue($flag)
    $colors=$flag.GetType().GetField('C',$members).GetValue($flag)
    $body=$null;$bodyPoint=$null;$bodyGain=0;$seen=[Collections.Generic.HashSet[string]]::new()
    $maxOld=0.0;$maxCorrected=0.0;$firstFailure=$null;$count=0
    for($i=$uv.Count-1;$i -ge 0;$i--){
        $p=$test.GetMethod('Design',$static).Invoke($null,@($uv[$i]))
        $key="{0},{1}" -f [math]::Round($p.x*10000),[math]::Round($p.y*10000)
        $depth=$test.GetMethod('Depth',$static).Invoke($null,@($p,$origin,[single]3,[single]2))
        if(-not $seen.Add($key) -or $depth -lt .4999){continue}
        # Recover the first suite's light-only residual directly, so this
        # diagnostic also runs after the test helper has been corrected/renamed.
        $light=$view.GetMethod('FloorLight',$static).Invoke($null,@($p.x,$p.y))
        $material=[UnityEngine.Color]::new($colors[$i].r/$light.r,$colors[$i].g/$light.g,$colors[$i].b/$light.b,1)
        $gain=$view.GetMethod('PavingCourtUse',$static).Invoke($null,@($p.x,$p.y))
        if($null -eq $body){$body=$material;$bodyPoint=$p;$bodyGain=$gain}
        foreach($channel in @('r','g','b')){
            $old=[math]::Abs($material.$channel-$body.$channel)
            $corrected=[math]::Abs($material.$channel/$gain-$body.$channel/$bodyGain)
            $maxOld=[math]::Max($maxOld,$old);$maxCorrected=[math]::Max($maxCorrected,$corrected)
            if($null -eq $firstFailure -and $old -gt .0001){
                $firstFailure=[ordered]@{vertex=$i;u=$p.x;v=$p.y;channel=$channel;actual=$material.$channel;
                    expected=$body.$channel;pointGain=$gain;bodyGain=$bodyGain;normalizedActual=$material.$channel/$gain;
                    normalizedExpected=$body.$channel/$bodyGain}
            }
        }
        $count++
    }
    $rows+=[ordered]@{origin=@($origin.x,$origin.y);bodyPoint=@($bodyPoint.x,$bodyPoint.y);samples=$count;
        maximumOldResidual=$maxOld;maximumNormalizedResidual=$maxCorrected;firstOldFailure=$firstFailure}
}
$json=$rows | ConvertTo-Json -Depth 8
$json | Set-Content -LiteralPath (Join-Path $repo 'ArtDirection/2026-10-14/foundry-court-cohesion-01/validation/first-suite/material-body-diagnostic.json')
$json
