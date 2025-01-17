# Sarif Annotator Github Action

Create annotations from SARIF file

## Usage

```yaml
- name: Annotate
  uses: LoremFooBar/sarif-annotator-action@v1
  env:
    SARIF_FILE_PATH: "inspect-code-results.sarif"
    # mandatory, needed because GITHUB_WORKSPACE is different when running containerized action https://github.com/actions/runner/issues/2058
    CLONE_DIR: ${{ github.workspace }}
    # mandatory, used to filter sarif file by diff
    PULL_REQUEST_NUMBER: ${{ github.event.number }}
    # mandatory, used to make api call to get pr diff
    GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
    # whether to fail when issues found (after filtering). default `false`
    FAIL_WHEN_ISSUES_FOUND: true
    # show debug logs
    DEBUG: true 
```

## License

[MIT License](LICENSE)
