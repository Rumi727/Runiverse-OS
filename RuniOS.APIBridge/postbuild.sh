#!/bin/sh

solutiondir="$1"
dllpath="$2"
name="$3"

# 대상 폴더: 솔루션 폴더 안의 Dlls
destdir="${solutiondir}dlls"

# 대상 폴더가 없으면 생성
if [ ! -e "$destdir" ]; then
    mkdir "$destdir"
fi

# .dll 파일 복사
if [ -e "${dllpath}${name}.dll" ]; then
    cp "${dllpath}${name}.dll" "$destdir/${name}.dll"
    echo "${name}.dll 복사 완료"
else
    echo
    echo "${name}.dll 파일을 찾을 수 없습니다."
fi

# .pdb 파일 복사
if [ -e "${dllpath}${name}.pdb" ]; then
    cp "${dllpath}${name}.pdb" "$destdir/${name}.pdb"
    echo "${name}.pdb 복사 완료"
else
    echo
    echo "${name}.pdb 파일을 찾을 수 없습니다."
fi

# .xml 파일 복사
if [ -e "${dllpath}${name}.xml" ]; then
    cp "${dllpath}${name}.xml" "$destdir/${name}.xml"
    echo "${name}.xml 복사 완료"
else
    echo
    echo "${name}.xml 파일을 찾을 수 없습니다."
fi

echo
echo "빌드 후 이벤트 완료"
