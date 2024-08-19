Forked version with add to queue for next video instead of instant replacing [umpvw-Q](https://github.com/M0nsh3r1/umpvw)

# umpvw-Q

umpvw-Q replicates mpv's macOS single-instance behavior on Windows, but with queue mode. The next video will be played after the previous one ends.

It is a wrapper for mpv that uses the player's JSON IPC capabilities, that are used to replace the currently playing file. 

It also handles selecting multiple items in File Explorer and trying to play them - to handle this, it does IPC with itself. Files are launched in alphabetical order, as there isn't really a way to predict what order things will land in. 

## Requirements

.NET Framework, whatever the latest one is ¯\\\_(ツ)_/¯

## Fix 15 item selection limit

Follow this guide: https://support.microsoft.com/help/2022295/
