@echo off

chcp 65001 > nul

set "solutiondir=%~1"
set "dllpath=%~2"
set "name=%~3"

rem 대상 폴더: 솔루션 폴더 안의 Dlls
set "destdir=%solutiondir%dlls"

rem 대상 폴더가 없으면 생성
if not exist "%destdir%" mkdir "%destdir%"

rem .dll 파일 복사
if exist "%dllpath%%name%.dll" (
    copy "%dllpath%%name%.dll" "%destdir%\%name%.dll"
    echo %name%.dll 복사 완료
) else (
    echo.
    echo %name%.dll 파일을 찾을 수 없습니다.
)

rem .pdb 파일 복사
if exist "%dllpath%%name%.pdb" (
    copy "%dllpath%%name%.pdb" "%destdir%\%name%.pdb"
    echo %name%.pdb 복사 완료
) else (
    echo.
    echo %name%.pdb 파일을 찾을 수 없습니다.
)

rem .xml 파일 복사
if exist "%dllpath%%name%.xml" (
    copy "%dllpath%%name%.xml" "%destdir%\%name%.xml"
    echo %name%.xml 복사 완료
) else (
    echo.
    echo %name%.xml 파일을 찾을 수 없습니다.
)

echo.
echo 빌드 후 이벤트 완료