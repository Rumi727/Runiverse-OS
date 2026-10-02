# Git와 registry authoring

GitPackage와 RegistryPackage는 exact definition을 사람이 작성하는 SO입니다. 원격 metadata에서 dependency 선언을 수집하지 않습니다.

1. logical package ID와 실제 native package name을 작성합니다. 같으면 native name override는 비웁니다.
2. Git은 URL/path/full commit, Registry는 exact version과 필요한 registry configuration을 작성합니다.
3. Package의 Dependencies에 exact dependency assets를 직접 연결합니다.
4. 전체 정의 inventory와 roots를 caller/UI에 전달합니다.
5. Core closure가 성공하면 caller가 구성한 InstallationRunner로 실행합니다.

Git URL이나 registry 이름만 보고 다른 Package definition을 자동 검색하지 않습니다. 두 roots가 같은 ID의 서로 다른 exact definitions를 요구하면 Core graph conflict입니다. UPM package.json 내부 dependencies는 UPM이 처리합니다.

GitPackage/RegistryPackage/LocalPackage는 UpmInstallation을 제공합니다. executor는 descriptor의 완결된 native reference를 전달하며 Package 종류를 다시 확인하지 않습니다. custom Package도 같은 UpmInstallation을 제공할 수 있습니다.

EmbeddedPackage는 기존 embedded package compatibility 전용입니다. Git/Registry 패키지를 embedded로 변환하는 Installation이 아닙니다.

자세한 API와 execution semantics는 ../README.md를 참고합니다.
