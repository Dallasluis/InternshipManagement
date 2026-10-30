// Turn the agent's final text into the JSON contract the API expects, or an error response.
const res = $input.first().json || {};
const fail = (statusCode, code) => [{ json: { statusCode, body: { error: code } } }];

if (res.error || typeof res.output !== 'string' || !res.output.trim()) {
  return fail(502, 'ai_unavailable');
}

let text = res.output.trim();
text = text.replace(/^```(?:json)?\s*/i, '').replace(/```\s*$/, '').trim();
const first = text.indexOf('{');
const last = text.lastIndexOf('}');
if (first === -1 || last <= first) {
  return fail(502, 'invalid_json_from_ai');
}
text = text.slice(first, last + 1);

let d;
try {
  d = JSON.parse(text);
} catch (e) {
  return fail(502, 'invalid_json_from_ai');
}
if (!d || typeof d !== 'object' || Array.isArray(d)) {
  return fail(502, 'invalid_shape_from_ai');
}

// The API checks every id and cleans the text again. This only drops wrong types and caps sizes.
const explanations = (Array.isArray(d.explanations) ? d.explanations : [])
  .filter((x) => x && typeof x === 'object' && !Array.isArray(x))
  .slice(0, 10)
  .map((x) => ({
    targetId: typeof x.targetId === 'number' || typeof x.targetId === 'string' ? x.targetId : null,
    explanation: typeof x.explanation === 'string' ? x.explanation.trim().slice(0, 500) : null
  }))
  .filter((x) => x.targetId !== null && x.explanation);

return [{ json: { statusCode: 200, body: { explanations } } }];
