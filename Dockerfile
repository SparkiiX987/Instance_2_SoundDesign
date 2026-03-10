# ─────────────────────────────────────────────────────────────
# Unity build image – Instance_2_SoundDesign
# Supports: StandaloneLinux64 | StandaloneWindows64
#
# ⚠️  L'activation de la licence Unity Personal est gérée
#     par le workflow GitHub Actions (game-ci/unity-activate),
#     PAS dans ce Dockerfile.
#     Ce Dockerfile ne fait que compiler le projet Unity.
#
# Usage local (après activation manuelle sur la machine) :
#   Linux :
#     docker build --build-arg BUILD_TARGET=StandaloneLinux64 -t unity-build .
#   Windows :
#     docker build --build-arg BUILD_TARGET=StandaloneWindows64 \
#       --build-arg UNITY_MODULE=windows-mono -t unity-build .
# ─────────────────────────────────────────────────────────────

ARG UNITY_VERSION=6000.3.10f1
ARG UNITY_MODULE=linux-il2cpp
ARG UNITY_IMAGE=unityci/editor:ubuntu-${UNITY_VERSION}-${UNITY_MODULE}-3

FROM ${UNITY_IMAGE}

ARG BUILD_TARGET=StandaloneLinux64
ARG BUILD_PATH=/build
ARG PROJECT_PATH=/project

ENV BUILD_TARGET=${BUILD_TARGET}
ENV BUILD_PATH=${BUILD_PATH}
ENV PROJECT_PATH=${PROJECT_PATH}

# ── Copy project ─────────────────────────────────────────────
COPY . ${PROJECT_PATH}

WORKDIR ${PROJECT_PATH}

# ── Run the build ────────────────────────────────────────────
# La licence est déjà activée sur la machine hôte ou via le workflow CI.
RUN /usr/bin/unity-editor \
      -batchmode \
      -quit \
      -nographics \
      -projectPath "${PROJECT_PATH}" \
      -buildTarget "${BUILD_TARGET}" \
      -executeMethod BuildScript.PerformBuild \
      -customBuildPath  "${BUILD_PATH}" \
      -customBuildTarget "${BUILD_TARGET}" \
      -logFile /dev/stdout

# ── Le build est dans /build ─────────────────────────────────
# Extraire après le build :
#   container_id=$(docker create unity-build)
#   docker cp "${container_id}:/build/." ./build/
#   docker rm "${container_id}"
# ─────────────────────────────────────────────────────────────
