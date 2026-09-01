const keys = [
  'gclid',
  'gbraid',
  'wbraid',
  'utm_source',
  'utm_medium',
  'utm_campaign',
  'utm_content',
  'utm_term',
  'campaignid',
  'adgroupid',
  'adid',
  'keyword',
  'matchtype',
  'device'
];

const googleAdsConversionId = import.meta.env.VITE_GOOGLE_ADS_CONVERSION_ID?.trim();
const googleAdsConversionLabel = import.meta.env.VITE_GOOGLE_ADS_CONVERSION_LABEL?.trim();

export function captureCampaignParams(): void {
  const params = new URLSearchParams(window.location.search);
  const stored = readCampaignParams();
  keys.forEach((key) => {
    const value = params.get(key);
    if (value) {
      stored[key] = value;
    }
  });
  sessionStorage.setItem('leadengine_campaign', JSON.stringify(stored));
}

export function readCampaignParams(): Record<string, string> {
  const raw = sessionStorage.getItem('leadengine_campaign') || localStorage.getItem('leadengine_campaign');
  if (!raw) return {};
  try {
    return JSON.parse(raw) as Record<string, string>;
  } catch {
    return {};
  }
}

export function pushDataLayer(event: Record<string, unknown>): void {
  window.dataLayer = window.dataLayer || [];
  window.dataLayer.push(event);
}

export function trackGoogleAdsConversion(): Promise<void> {
  return new Promise((resolve) => {
    if (!googleAdsConversionId || !googleAdsConversionLabel) {
      resolve();
      return;
    }

    if (!window.gtag) {
      resolve();
      return;
    }

    let resolved = false;
    const finish = () => {
      if (resolved) return;
      resolved = true;
      resolve();
    };

    window.setTimeout(finish, 1500);
    window.gtag('event', 'conversion', {
      send_to: `${googleAdsConversionId}/${googleAdsConversionLabel}`,
      event_callback: finish,
      event_timeout: 1500
    });
  });
}

export function buildOrigem() {
  const params = readCampaignParams();
  return {
    gclid: params.gclid,
    gbraid: params.gbraid,
    wbraid: params.wbraid,
    utmSource: params.utm_source,
    utmMedium: params.utm_medium,
    utmCampaign: params.utm_campaign,
    utmContent: params.utm_content,
    utmTerm: params.utm_term,
    campaignId: params.campaignid,
    adGroupId: params.adgroupid,
    adId: params.adid,
    keyword: params.keyword,
    matchType: params.matchtype,
    device: params.device,
    landingPage: window.location.pathname,
    referrer: document.referrer,
    userAgent: navigator.userAgent
  };
}
