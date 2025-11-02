@echo off
echo ========================================
echo 角色静止动画生成工具
echo ========================================
echo.

REM 检查Python是否安装
python --version >nul 2>&1
if errorlevel 1 (
    echo [错误] 未找到Python，请先安装Python 3.6或更高版本
    echo 下载地址: https://www.python.org/downloads/
    pause
    exit /b 1
)

echo [信息] Python已安装
echo.

REM 检查Pillow是否安装
python -c "import PIL" >nul 2>&1
if errorlevel 1 (
    echo [信息] 正在安装Pillow库...
    pip install Pillow
    if errorlevel 1 (
        echo [错误] Pillow安装失败，请手动运行: pip install Pillow
        pause
        exit /b 1
    )
    echo [成功] Pillow安装完成
    echo.
)

echo [信息] 开始生成动画帧...
echo.

REM 运行脚本
python generate_idle_animation.py

if errorlevel 1 (
    echo.
    echo [错误] 脚本执行失败
    pause
    exit /b 1
)

echo.
echo ========================================
echo 动画帧生成完成！
echo ========================================
pause

