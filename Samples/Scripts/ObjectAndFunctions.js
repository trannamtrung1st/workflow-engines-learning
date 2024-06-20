import { Add2Numbers, Random } from 'FB.Functions'

export function Execute(INPUTS, OUTPUTS) {
    let { Input, Result } = { ...INPUTS, ...OUTPUTS };

    function WRAP() {
        // [START] User content
        const addResult = Add2Numbers({ X: Input.X, Y: Input.Y }, { Result: null });
        const randomResult = Random(null, { Result: null });
        Result = addResult.Result + randomResult.Result;
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
