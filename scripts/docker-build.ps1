param (
    [Parameter(Mandatory)]
    $Version
)

docker build --rm -t loremfoobar/sarif-annotator-action:$Version .
