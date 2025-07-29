; NSIS Script for Multi Serial Monitor Installer
; Q WAVE COMPANY LIMITED
; Version 1.0.0

;--------------------------------
; Include Modern UI
!include "MUI2.nsh"

;--------------------------------
; General Settings
Name "Multi Serial Monitor"
OutFile "MultiSerialMonitor_Setup_v1.0.0.exe"
Unicode True

; Installation directory
InstallDir "$PROGRAMFILES64\Q WAVE\Multi Serial Monitor"
InstallDirRegKey HKCU "Software\QWAVE\MultiSerialMonitor" ""

; Request application privileges
RequestExecutionLevel admin

; Compression
SetCompressor /SOLID lzma
SetCompressorDictSize 32

;--------------------------------
; Interface Settings
!define MUI_ABORTWARNING
!define MUI_ICON "favicon.ico"
!define MUI_UNICON "favicon.ico"

; Header image
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP "QW LOGO Qwave.png"
!define MUI_HEADERIMAGE_RIGHT

; Welcome page
!define MUI_WELCOMEPAGE_TITLE "Welcome to Multi Serial Monitor Setup"
!define MUI_WELCOMEPAGE_TEXT "This wizard will guide you through the installation of Multi Serial Monitor.$\r$\n$\r$\nMulti Serial Monitor is a professional serial port and Telnet monitoring application developed by Q WAVE COMPANY LIMITED.$\r$\n$\r$\nClick Next to continue."

; Finish page
!define MUI_FINISHPAGE_RUN "$INSTDIR\MultiSerialMonitor.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Launch Multi Serial Monitor"
!define MUI_FINISHPAGE_LINK "Visit Q WAVE COMPANY LIMITED website"
!define MUI_FINISHPAGE_LINK_LOCATION "https://qwave.co.th"

;--------------------------------
; Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE.txt"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

;--------------------------------
; Languages
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Thai"

;--------------------------------
; Version Information
VIProductVersion "1.0.0.0"
VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductName" "Multi Serial Monitor"
VIAddVersionKey /LANG=${LANG_ENGLISH} "Comments" "Professional Serial Port and Telnet Monitoring Application"
VIAddVersionKey /LANG=${LANG_ENGLISH} "CompanyName" "Q WAVE COMPANY LIMITED"
VIAddVersionKey /LANG=${LANG_ENGLISH} "LegalCopyright" "© 2025 Q WAVE COMPANY LIMITED. All rights reserved."
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileDescription" "Multi Serial Monitor Installer"
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileVersion" "1.0.0.0"
VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductVersion" "1.0.0.0"
VIAddVersionKey /LANG=${LANG_ENGLISH} "InternalName" "MultiSerialMonitor"
VIAddVersionKey /LANG=${LANG_ENGLISH} "LegalTrademarks" ""
VIAddVersionKey /LANG=${LANG_ENGLISH} "OriginalFilename" "MultiSerialMonitor_Setup_v1.0.0.exe"

;--------------------------------
; Installer Sections

Section "Multi Serial Monitor (Required)" SecMain
  SectionIn RO
  
  ; Set output path
  SetOutPath "$INSTDIR"
  
  ; Add files from the published output
  File "MultiSerialMonitor.exe"
  File "QW LOGO Qwave.png"
  File "favicon.ico"
  
  ; Store installation folder
  WriteRegStr HKCU "Software\QWAVE\MultiSerialMonitor" "" $INSTDIR
  
  ; Create uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  
  ; Register uninstaller in Add/Remove Programs
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiSerialMonitor" "DisplayName" "Multi Serial Monitor"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiSerialMonitor" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiSerialMonitor" "QuietUninstallString" "$\"$INSTDIR\Uninstall.exe$\" /S"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiSerialMonitor" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiSerialMonitor" "DisplayIcon" "$INSTDIR\favicon.ico"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiSerialMonitor" "Publisher" "Q WAVE COMPANY LIMITED"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiSerialMonitor" "DisplayVersion" "1.0.0"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiSerialMonitor" "URLInfoAbout" "https://qwave.co.th"
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiSerialMonitor" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiSerialMonitor" "NoRepair" 1
  
  ; Estimate size (in KB) - approximately 50MB
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiSerialMonitor" "EstimatedSize" 51200
  
SectionEnd

Section "Desktop Shortcut" SecDesktop
  CreateShortcut "$DESKTOP\Multi Serial Monitor.lnk" "$INSTDIR\MultiSerialMonitor.exe" "" "$INSTDIR\favicon.ico"
SectionEnd

Section "Start Menu Shortcuts" SecStartMenu
  CreateDirectory "$SMPROGRAMS\Q WAVE\Multi Serial Monitor"
  CreateShortcut "$SMPROGRAMS\Q WAVE\Multi Serial Monitor\Multi Serial Monitor.lnk" "$INSTDIR\MultiSerialMonitor.exe" "" "$INSTDIR\favicon.ico"
  CreateShortcut "$SMPROGRAMS\Q WAVE\Multi Serial Monitor\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
SectionEnd

Section "Quick Launch Shortcut" SecQuickLaunch
  CreateShortcut "$QUICKLAUNCH\Multi Serial Monitor.lnk" "$INSTDIR\MultiSerialMonitor.exe" "" "$INSTDIR\favicon.ico"
SectionEnd

;--------------------------------
; Component Descriptions
LangString DESC_SecMain ${LANG_ENGLISH} "The main Multi Serial Monitor application files (required)."
LangString DESC_SecDesktop ${LANG_ENGLISH} "Create a shortcut on the desktop."
LangString DESC_SecStartMenu ${LANG_ENGLISH} "Create shortcuts in the Start Menu."
LangString DESC_SecQuickLaunch ${LANG_ENGLISH} "Create a shortcut in the Quick Launch toolbar."

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecMain} $(DESC_SecMain)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecDesktop} $(DESC_SecDesktop)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecStartMenu} $(DESC_SecStartMenu)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecQuickLaunch} $(DESC_SecQuickLaunch)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;--------------------------------
; Uninstaller Section

Section "Uninstall"
  ; Remove files
  Delete "$INSTDIR\MultiSerialMonitor.exe"
  Delete "$INSTDIR\QW LOGO Qwave.png"
  Delete "$INSTDIR\favicon.ico"
  Delete "$INSTDIR\Uninstall.exe"
  
  ; Remove directories if empty
  RMDir "$INSTDIR"
  RMDir "$PROGRAMFILES64\Q WAVE"
  
  ; Remove shortcuts
  Delete "$DESKTOP\Multi Serial Monitor.lnk"
  Delete "$QUICKLAUNCH\Multi Serial Monitor.lnk"
  Delete "$SMPROGRAMS\Q WAVE\Multi Serial Monitor\Multi Serial Monitor.lnk"
  Delete "$SMPROGRAMS\Q WAVE\Multi Serial Monitor\Uninstall.lnk"
  RMDir "$SMPROGRAMS\Q WAVE\Multi Serial Monitor"
  RMDir "$SMPROGRAMS\Q WAVE"
  
  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiSerialMonitor"
  DeleteRegKey HKCU "Software\QWAVE\MultiSerialMonitor"
  DeleteRegKey /ifempty HKCU "Software\QWAVE"
  
  ; Remove user data (optional - ask user)
  MessageBox MB_YESNO|MB_ICONQUESTION "Do you want to remove all user settings and data files?" IDNO +3
  RMDir /r "$APPDATA\Q WAVE\Multi Serial Monitor"
  RMDir "$APPDATA\Q WAVE"
  
SectionEnd

;--------------------------------
; Functions

Function .onInit
  ; Check if already installed
  ReadRegStr $R0 HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiSerialMonitor" "UninstallString"
  StrCmp $R0 "" done
  
  MessageBox MB_OKCANCEL|MB_ICONEXCLAMATION \
  "Multi Serial Monitor is already installed. $\n$\nClick `OK` to remove the previous version or `Cancel` to cancel this upgrade." \
  IDOK uninst
  Abort
  
  uninst:
    ClearErrors
    ExecWait '$R0 _?=$INSTDIR'
    
    IfErrors no_remove_uninstaller done
    no_remove_uninstaller:
  
  done:
FunctionEnd

Function un.onInit
  MessageBox MB_ICONQUESTION|MB_YESNO|MB_DEFBUTTON2 "Are you sure you want to completely remove Multi Serial Monitor and all of its components?" IDYES +2
  Abort
FunctionEnd

Function un.onUninstSuccess
  HideWindow
  MessageBox MB_ICONINFORMATION|MB_OK "Multi Serial Monitor was successfully removed from your computer.$\n$\nThank you for using Q WAVE products!"
FunctionEnd