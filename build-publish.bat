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

echo === 3. Publish backend (tu goi san .NET Runtime, khong can cai gi tren may khac) ===
dotnet publish vovinam-backend.csproj -c Release -o %OUTPUT_DIR% -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
if errorlevel 1 (
    echo Publish backend that bai. Dung lai.
    exit /b 1
)

echo.
echo Xong. File chay nam trong %OUTPUT_DIR%\vovinam-backend.exe -- tu goi
echo san .NET Runtime ben trong, may khac KHONG can cai .NET gi ca, chi
echo can copy dung thu muc %OUTPUT_DIR% roi chay thang file .exe.
echo Bo doi lai, bo publish nay nang hon han (gop ca Runtime ~60-100MB).
echo Nho copy kem file vovinam.db neu da co du lieu, va wwwroot\uploads
echo neu co anh VDV/logo, khi mang sang may khac.
endlocal
