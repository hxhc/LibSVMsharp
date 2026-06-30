#!/usr/bin/env bash
# Builds the native libsvm shared library for Linux (x64 or arm64) and installs it
# under LibSVMsharp/runtimes/<rid>/native/ so the runtime resolver loads the
# architecture matching the process automatically.
#
# Usage:
#   ./build-native.sh            # build for the host architecture
#   ./build-native.sh x64        # build for x86-64 (native or cross)
#   ./build-native.sh arm64      # build for aarch64 (native or cross)
#
# Requirements: wget or curl, tar, make, and a C/C++ compiler. For arm64 cross-compilation on
# an x86-64 host, install the aarch64 cross toolchain, e.g. on Debian/Ubuntu:
#   sudo apt install gcc-aarch64-linux-gnu g++-aarch64-linux-gnu
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEST_ROOT="$REPO_ROOT/LibSVMsharp"
WORKDIR="$(mktemp -d)"
trap 'rm -rf "$WORKDIR"' EXIT

HOST_ARCH="$(uname -m)"   # x86_64 / aarch64 / arm64

# Decide target architecture.
case "${1:-}" in
  x64)   TARGET_ARCH=x64 ;;
  arm64) TARGET_ARCH=arm64 ;;
  "")
    case "$HOST_ARCH" in
      x86_64) TARGET_ARCH=x64 ;;
      *)      TARGET_ARCH=arm64 ;;
    esac
    ;;
  *) echo "Unknown arch '$1'. Use 'x64' or 'arm64'." >&2; exit 2 ;;
esac

# Pick RID + cross-compile toolchain prefix.
case "$TARGET_ARCH" in
  x64)
    RID="linux-x64"
    CROSS=""
    ;;
  arm64)
    RID="linux-arm64"
    if [[ "$HOST_ARCH" == "aarch64" || "$HOST_ARCH" == "arm64" ]]; then
      CROSS=""                       # native build on an aarch64 host
    else
      CROSS="aarch64-linux-gnu-"     # cross-compile from x86_64
    fi
    ;;
esac

echo "==> Target: $RID   (host: $HOST_ARCH, cross prefix: '${CROSS:-none}')"

LIBSVM_VERSION="3.37"
LIBSVM_TARBALL="libsvm-${LIBSVM_VERSION}.tar.gz"
LIBSVM_URL="https://www.csie.ntu.edu.tw/~cjlin/libsvm/${LIBSVM_TARBALL}"
echo "==> Downloading libsvm ${LIBSVM_VERSION} from ${LIBSVM_URL}"
if command -v wget >/dev/null 2>&1; then
    wget -q -O "$WORKDIR/${LIBSVM_TARBALL}" "${LIBSVM_URL}"
elif command -v curl >/dev/null 2>&1; then
    curl -fsSL -o "$WORKDIR/${LIBSVM_TARBALL}" "${LIBSVM_URL}"
else
    echo "ERROR: need wget or curl to download libsvm source" >&2
    exit 3
fi

echo "==> Extracting libsvm ${LIBSVM_VERSION} into $WORKDIR"
tar -xzf "$WORKDIR/${LIBSVM_TARBALL}" -C "$WORKDIR"
# tarball extracts to libsvm-<version>/; rename so the rest of the script is version-agnostic
mv "$WORKDIR/libsvm-${LIBSVM_VERSION}" "$WORKDIR/libsvm"

echo "==> Building libsvm shared library"
# libsvm's Makefile honors CC/CXX and its 'lib' target produces libsvm.so.<SHVER>.
make -C "$WORKDIR/libsvm" clean
make -C "$WORKDIR/libsvm" lib CC="${CROSS}gcc" CXX="${CROSS}g++"

DEST="$DEST_ROOT/runtimes/$RID/native"
mkdir -p "$DEST"
find "$WORKDIR/libsvm" -maxdepth 1 -name 'libsvm.so*' -exec cp -f {} "$DEST/" \;

# Ensure a plain libsvm.so exists so the resolver can always load it by name.
if [[ ! -f "$DEST/libsvm.so" ]]; then
    VERSIONED_SO="$(find "$DEST" -maxdepth 1 -name 'libsvm.so.*' | head -n1)"
    [[ -n "$VERSIONED_SO" ]] && cp -f "$VERSIONED_SO" "$DEST/libsvm.so"
fi

echo "==> Installed into $DEST:"
ls -1 "$DEST"/libsvm.so* 2>/dev/null || true
echo ""
echo "Done. Build both architectures by running the script twice, then:"
echo "    cd \"$REPO_ROOT\" && dotnet build LibSVMsharp.sln"
