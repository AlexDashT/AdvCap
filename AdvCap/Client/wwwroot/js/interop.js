window.addEventListener('beforeunload', (event) => {
    // Call the .NET method to save the state
    DotNet.invokeMethodAsync('AdvCap.Client', 'SaveStateOnExit');
});
