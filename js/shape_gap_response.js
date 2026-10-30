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

// The API only accepts ids it sent and cleans the text again. This drops wrong types and caps sizes.
const summary = typeof d.summary === 'string' ? d.summary.trim().slice(0, 700) : null;
const items = (Array.isArray(d.items) ? d.items : [])
  .filter((x) => x && typeof x === 'object' && !Array.isArray(x))
  .slice(0, 10)
  .map((x) => ({
    id: typeof x.id === 'string' || typeof x.id === 'number' ? x.id : null,
    why: typeof x.why === 'string' ? x.why.trim().slice(0, 400) : null
  }))
  .filter((x) => x.id !== null && x.why);

return [{ json: { statusCode: 200, body: { summary, items } } }];
