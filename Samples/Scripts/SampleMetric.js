import { LastSeriesBefore } from 'FB.Functions'

export async function Execute(INPUTS, OUTPUTS) {
    let { Metric, Snapshot, Previous } = { ...INPUTS, ...OUTPUTS };

    async function WRAP() {
        // [START] User content
        const { Value, Timestamp } = Metric.Snapshot;
        const { Result: prevSeries } = await LastSeriesBefore({ InputMetric: Metric, BeforeTime: Timestamp }, { Result: null });
        Snapshot = Value;
        Previous = prevSeries.Value;
        // [END] User content
    }

    const WRAP_RESULT = await WRAP();
    if (WRAP_RESULT !== undefined) return WRAP_RESULT;
    const ENGINE_RESULT = { Snapshot, Previous };
    Object.keys(ENGINE_RESULT).forEach(key => {
        if (ENGINE_RESULT[key] === undefined) {
            delete ENGINE_RESULT[key];
        }
    });
    return ENGINE_RESULT;
}
