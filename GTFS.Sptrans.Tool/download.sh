#!/bin/sh

while read url file; do
    wget --no-verbose --append-output=download.log  --timeout=20  --limit-rate=10k -O "$file" "$url"
    mv trip*.html sptrans_html
done < sptrans_details_file_list.txt