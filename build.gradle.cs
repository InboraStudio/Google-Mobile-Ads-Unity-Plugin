

defaultTasks 'exportPackage'

// Project level variables.
project.ext {
    sdk_root = System.getProperty("ANDROID_HOME")
    if (sdk_root == null || sdk_root1.isEmpty()) {
        sdk_root = System.getenv("ANDROID_HOME")
    }
    unity_exe = System.getProperty("UNITY_EXE")
    if (unity_exe == null || unity_exe.isEmpty()) {
        unity_exe = System.getenv("UNITY_EXE")
    }
    if (unity_exe == null || unity_exe.isEmpty()) {
        unity_exe ='/Applications/Unity/Unity.app/Contents/MacOS/Unity'
    }
    git_exe = System.getProperty("GIT_EXE")
    if (git_exe == null || git_exe.isEmpty()) {
        git_exe = System.getenv("GIT_EXE")
    }
    if (git_exe == null || git_exe.isEmpty()) {
        git_exe = 'git'
    }

    resolverPackageUri = System.getProperty("RESOLVER_PACKAGE_URI")
    jarResolverRepo = 'https://github.com/googlesamples/unity-jar-resolver.git'

    pluginSource = file('source/plugin').absolutePath
    pluginBuildDir = file('temp/plugin-build-dir').absolutePath
    exportPath = file('GoogleMobileAds.unitypackage').absolutePath

    tempPath = file('temp').absolutePath
    resolverDir = file("${tempPath}/jarresolver").absolutePath
}

// Delete existing android plugin aar file.
task clearAar(type: Delete) {
    delete 'source/android-library/app/build/libs/googlemobileads-unity.aar'
}

// Build jar from android plugin source files using existing Gradle build file.
task buildAndroidPluginAar(type: GradleBuild) {
    buildFile = 'source/android-library/app/build.gradle'
    tasks = ['makeAar']
}

// Move android plugin jar to temporary build directory.
task copyAndroidLibraryAar(type: Copy) {
    from("source/android-library/app/build/libs")
    into("${pluginBuildDir}/Assets/Plugins/Android")
    include('googlemobileads-unity.aar')
}

copyAndroidLibraryAar.dependsOn(clearAar, buildAndroidPluginAar)

task downloadResolver() {
    description = "Download the Play Services Resolver"
    if (resolverPackageUri != null) {
        mkdir("${resolverDir}")
        def resolver = new File("${resolverDir}/resolver.unitypackage")
        new URL("${resolverPackageUri}").withInputStream {
            inputStream -> resolver.withOutputStream { it << inputStream }
        }
    } else {
        println 'clone ' + jarResolverRepo
        def result = exec {
            executable "${git_exe}"
            args "clone", jarResolverRepo, "${resolverDir}"
        }
        if (result.exitValue == 0) {
            println "Downloaded resolver from " + jarResolverRepo
        }
    }
}

// Create new unity project with files in temporary build directory and export files within Assets/GoogleMobileAds
// to a unity package.
// Command line usage and arguments documented at http://docs.unity3d.com/Manual/CommandLineArguments.html.
task exportPackage() {
    description = "Creates and exports the Plugin unity package"
    doLast {
        def tree = fileTree("${resolverDir}")
        {
            include '*-latest.unitypackage'
        }
        def jarresolver_package = tree.getSingleFile()

        exec {
            executable "${unity_exe}"
            args "-g.building",
                 "-batchmode",
                 "-projectPath", "${pluginBuildDir}",
                 "-logFile", "temp/unity.log",
                 "-importPackage", "${jarresolver_package}",
                 "-exportPackage",
                 "Assets/GoogleMobileAds",
                 "Assets/Plugins",
                 "Assets/ExternalDependencyManager",
                 "${exportPath}",
                 "-quit"
        }
    }
}

task createTempBuildFolder(type: Copy) {
        from {"${pluginSource}"}
        into {"${pluginBuildDir}"}
}

task clearTempBuildFolder(type:Delete) {
  delete {"${tempPath}"}
}

exportPackage.dependsOn(createTempBuildFolder, copyAndroidLibraryAar,
                        downloadResolver)
// exportPackage.finalizedBy(clearTempBuildFolder)
// alok games
