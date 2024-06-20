
export async function Execute(INPUTS, OUTPUTS) {
    let { Ms } = { ...INPUTS, ...OUTPUTS };
    async function WRAP() {
        // [START] User content
        await FB.DelayAsync(Ms);
        while (true) { var a = 1; }
        // [END] User content
    }
    const WRAP_RESULT = await WRAP();
}
