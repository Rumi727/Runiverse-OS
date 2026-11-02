#!/bin/sh

solutiondir="$1"
dllpath="$2"
name="$3"

# 대상 폴더: 솔루션 폴더 안의 Dlls
destdir="${solutiondir}dlls"

# 프로젝트 대상 폴더: UIElementsModule은 uielements 패키지, 에디터는 core.editor 패키지, 나머지는 core 패키지
case "$name" in
    *.UIElementsModule) package="io.github.rumi727.runios.uielements" ;;
    RuniOS.Editor.*) package="io.github.rumi727.runios.core.editor" ;;
    *) package="io.github.rumi727.runios.core" ;;
esac

# 예: RuniOS.Editor.APIBridge.UnityEngine.CoreModule -> RuniOS.Editor.APIBridge
projectdestdir="${solutiondir}../Packages/${package}/Plugins/${name%%.Unity*}"

copy() {
    # 대상 폴더가 없으면 생성
    if [ ! -e "$2" ]; then
        mkdir -p "$2"
    fi

    if [ -e "${dllpath}${name}.$1" ]; then
        cp "${dllpath}${name}.$1" "$2/${name}.$1"
        echo "${name}.$1 -> $2 복사 완료"
    else
        echo
        echo "${name}.$1 파일을 찾을 수 없습니다."
    fi
}

for ext in dll pdb xml; do
    copy "$ext" "$destdir"

    # 프로젝트에서 쓰지 않는 브릿지도 있으므로, 이미 프로젝트에 있는 파일만 덮어씀
    if [ -e "$projectdestdir/${name}.$ext" ]; then
        copy "$ext" "$projectdestdir"
    fi
done

echo
echo "빌드 후 이벤트 완료"
