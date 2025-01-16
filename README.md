# Sarif Annotator Github Action

Create annotations from SARIF file

## Usage

```yaml
- name: Annotate
  uses: LoremFooBar/sarif-annotator-action@v1
  env:
    SARIF_FILE_PATH: "inspect-code-results.sarif"
    PULL_REQUEST_NUMBER: ${{ github.event.number }} # mandatory, used to filter sarif file by diff
    GH_TOKEN: ${{ secrets.GITHUB_TOKEN }} # mandatory, used to make api call to get pr diff
    FAIL_WHEN_ISSUES_FOUND: true # whether to fail when issues found (after filtering)
    DEBUG: true # show debug logs
```

## License

[MIT License](LICENSE)
