#!/bin/bash
metaFormat="*.mat"
for var in "${@:2}"
do
	# $var は .meta パス
	for guid in $(grep "guid: \([0-9]\|[a-z]\)" "$var")
	do
		if [ "$guid" != "guid:" ]
			then 
			echo $guid
			find . 2> /dev/null -type f -name "*$1" | xargs -n 10 grep -ln 2> /dev/null "$guid"
		fi

	done
done

exit 0