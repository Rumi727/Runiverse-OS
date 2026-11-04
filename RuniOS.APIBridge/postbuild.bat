@echo off

chcp 65001 > nul

set "solutiondir=%~1"
set "dllpath=%~2"
set "name=%~3"

rem 대상 폴더: 솔루션 폴더 안의 Dlls
set "destdir=%solutiondir%dlls"

rem 프로젝트 대상 폴더: UIElementsModule은 uielements 패키지, 에디터는 core.editor 패키지, 나머지는 core 패키지
set "package=io.github.rumi727.runios.core"
if "%name:~0,14%"=="RuniOS.Editor." set "package=io.github.rumi727.runios.core.editor"
if "%name:~-17%"==".UIElementsModule" set "package=io.github.rumi727.runios.uielements"

rem 예: RuniOS.Editor.APIBridge.UnityEngine.CoreModule -> RuniOS.Editor.APIBridge
set "rest=%name:*.Unity=%"
call set "folder=%%name:.Unity%rest%=%%"

set "projectdestdir=%solutiondir%..\Packages\%package%\Plugins\%folder%"

rem 프로젝트에서 쓰지 않는 브릿지도 있으므로, 이미 프로젝트에 있는 파일만 덮어씀
for %%e in (dll pdb xml) do (
    call :copyfile %%e "%destdir%"
    if exist "%projectdestdir%\%name%.%%e" call :copyfile %%e "%projectdestdir%"
)

echo.
echo 빌드 후 이벤트 완료
exit /b

:copyfile
rem 대상 폴더가 없으면 생성
if not exist "%~2" mkdir "%~2"

if exist "%dllpath%%name%.%~1" (
    copy "%dllpath%%name%.%~1" "%~2\%name%.%~1"
    echo %name%.%~1 -^> %~2 복사 완료
) else (
    echo.
    echo %name%.%~1 파일을 찾을 수 없습니다.
)
exit /b
