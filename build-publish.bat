@echo off
REM Xay frontend, gop vao wwwroot, roi publish backend thanh 1 bo file
REM .exe hoan chinh -- chay dung 1 lenh nay thay vi lam tay tung buoc.
REM Gia dinh vovinam-frontend nam cung cap voi vovinam-backend (cung thu
REM muc Project cha) -- doi lai FRONTEND_DIR ben duoi neu khac.

setlocal
set FRONTEND_DIR=..\vovinam-frontend
set OUTPUT_DIR=bin\publish

echo === 1. Build frontend ===
pushd %FRONTEND_DIR%
call npm run build
if errorlevel 1 (
    echo Build frontend that bai. Dung lai.
    popd
    exit /b 1
)
popd

echo === 2. Xoa wwwroot cu, chep ban build moi vao ===
if exist wwwroot rmdir /s /q wwwroot
mkdir wwwroot
xcopy /e /i /y "%FRONTEND_DIR%\dist\*" wwwroot\

echo === 3. Publish backend ===
dotnet publish -c Release -o %OUTPUT_DIR%
if errorlevel 1 (
    echo Publish backend that bai. Dung lai.
    exit /b 1
)

echo.
echo Xong. File chay nam trong %OUTPUT_DIR%\vovinam-backend.exe
echo Nho copy ca thu muc %OUTPUT_DIR% khi mang sang may khac -- kem file
echo vovinam.db neu da co du lieu, va wwwroot\uploads neu co anh VDV.
endlocal
