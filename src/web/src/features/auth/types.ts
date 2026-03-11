export type SubmissionState = {
  status: 'idle' | 'loading' | 'success' | 'error'
  message: string | null
}

export const IDLE_STATE: SubmissionState = {
  status: 'idle',
  message: null,
}
