export function Execute(INPUTS, OUTPUTS) {
    let { } = { ...INPUTS, ...OUTPUTS };
    function WRAP() {
        // [START] User content
        const a = 2;
        /* Test position */ FB.DemoException();
        let b = 5;
        // [END] User content
    }
    const WRAP_RESULT = WRAP();
}
