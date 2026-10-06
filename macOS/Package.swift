// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "OpenMDMac",
    platforms: [.macOS(.v14)],
    products: [.executable(name: "OpenMD", targets: ["OpenMD"])],
    targets: [.executableTarget(name: "OpenMD", path: "Sources/OpenMD", resources: [.process("Resources")])]
)
