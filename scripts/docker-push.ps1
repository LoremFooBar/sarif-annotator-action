param (
    [Parameter(Mandatory)]
    $Version
)

docker push loremfoobar/sarif-annotator-action:$Version
