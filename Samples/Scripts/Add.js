
export function Execute(INPUTS, OUTPUTS) {
    let { X, Y, Result } = { ...INPUTS, ...OUTPUTS };

    function WRAP() {
        // [START] User content
        Result = X + Y
        // [END] User content
    }

    const WRAP_RESULT = WRAP();
    if (WRAP_RESULT !== undefined) return WRAP_RESULT;
    const ENGINE_RESULT = { Result };
    Object.keys(ENGINE_RESULT).forEach(key => {
        if (ENGINE_RESULT[key] === undefined) {
            delete ENGINE_RESULT[key];
        }
    });
    return ENGINE_RESULT;
}
