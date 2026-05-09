const BASE_URL = '/api'

export async function apiFetch<T>(path: string, options?: RequestInit): Promise<T> {
  const headers: Record<string, string> = { ...options?.headers as Record<string, string> }
  if (options?.body) {
    headers['Content-Type'] = headers['Content-Type'] ?? 'application/json'
  }

  const response = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers
  })

  if (!response.ok) {
    const errorText = await response.text()
    throw new Error(`API error ${response.status}: ${errorText}`)
  }

  return response.json()
}
