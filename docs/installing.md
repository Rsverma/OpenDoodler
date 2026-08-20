# Installing OpenDoodler

This covers installing OpenDoodler from the `.msi` installer. If you'd rather build and run it
from source instead, see the "How To Use" section in the main [README](../README.md).

## 1. Get the installer

Download `OpenDoodlerSetup.msi` from the project's [Releases](https://github.com/Rsverma/OpenDoodler/releases)
page.

If there's no release available yet, or you want the latest unreleased changes, you (or anyone
with the repo cloned) can build it yourself - see [`installer/README.md`](../installer/README.md).

## 2. Run it

Double-click `OpenDoodlerSetup.msi`.

- **Windows may show a "Windows protected your PC" SmartScreen warning.** This happens because
  the installer isn't code-signed (a paid certificate that's out of scope for this project right
  now) - it doesn't mean anything is wrong. Click **More info**, then **Run anyway**.
- **Windows will ask for administrator permission (UAC prompt).** This is expected - OpenDoodler
  installs to `Program Files`, which requires it. Click **Yes**.

## 3. Walk through the setup wizard

1. **License** - OpenDoodler is licensed under the GNU Affero General Public License v3.0
   (AGPL-3.0). Accept it to continue.
2. **Install location** - defaults to `C:\Program Files\OpenDoodler\`. Change it here if you want
   it somewhere else, or just click **Next**.
3. **Install** - click **Install** (this is the step that triggers the UAC prompt above, if it
   hasn't already appeared).
4. **Finish** - once it completes, click **Finish**.

## 4. Launch it

Open the Start menu and search for **OpenDoodler**, or find it under
Start &gt; All apps &gt; OpenDoodler. There's no desktop shortcut by default.

## Updating to a newer version

Just run the newer `OpenDoodlerSetup.msi` - it automatically removes the old version first, so
there's no need to uninstall manually in between.

## Uninstalling

Settings &gt; Apps &gt; Installed apps (or the older Control Panel &gt; Programs and Features),
find **OpenDoodler**, and choose **Uninstall**.

Uninstalling only removes the installed app files and the Start Menu shortcut. Your projects
(`.obap` files, wherever you saved them) and your local graphics library
(`%LocalAppData%\OpenBoardAnim.db`) are left alone, so they'll still be there if you reinstall
later.
