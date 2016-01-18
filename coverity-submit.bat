@echo off
echo Submitting to coverity
curl --form token=lakDbc3WyTLmTh6pu0CECg --form email=michal.kuncl@gmail.com --form file=@coverity-GitVersioner.zip --form version="$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$" --form description="$Branch$:$MajorVersion$.$MinorVersion$.$Revision$-$Commit$-$ShortHash$" https://scan.coverity.com/builds?project=Pavuucek%2FGitVersioner
echo FINISHED!