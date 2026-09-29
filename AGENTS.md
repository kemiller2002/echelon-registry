# Agent Guide

## CI observation discipline

Commit and push incremental recovery points. Keep working after pushes when independent work remains. Do not wait for remote CI after every push. Check remote CI at the final implementation boundary by default, or earlier only when the result is needed to proceed safely. Ordinary commit-driven CI should wait for a 10-minute quiet period so nearby commits batch together.
