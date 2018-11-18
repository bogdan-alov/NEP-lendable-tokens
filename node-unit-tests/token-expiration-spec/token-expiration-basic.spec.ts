import * as expect from "expect";
import NeoJs from "../NeoJs";
import Aigle from "aigle";
import * as _ from 'lodash';

let neo = new NeoJs({
    scriptHash: '6787cccc527207d928e01b32c6b98c4860bfddfd' //token expiration #13.11.18/10:27 fixed ownerof
});
let addressAsByteArray = neo.sc.ContractParam.byteArray(neo.config.myAddress, 'address');
let otherAddress = neo.sc.ContractParam.byteArray('AHZrkZtNB1n61vthphomGsrhd7deWMYBmi', 'address');
let hasMinted = false;
describe("Token Expiration", function () {
    this.timeout(50000);
    before(async () => {
        // await neo.call('mintToken', [neo.sc.ContractParam.byteArray(neo.config.myAddress, 'address')]);

        let result = await neo.get('totalSupply', []);
        if (result[0].value === '') {
            console.log('MINTING 4 TOKENS!!!');
            await neo.call('mintToken', [addressAsByteArray]);
            await neo.call('mintToken', [addressAsByteArray]);
            await neo.call('mintToken', [addressAsByteArray]);
            await neo.call('mintToken', [addressAsByteArray]);
            hasMinted = true;
        }
    });
    it('should return 04 balanceOf for my address', async () => {
        let result = await neo.get('balanceOf', [addressAsByteArray]);
        expect(result[0].value).toEqual('04');
    });
    it('should return empty balanceOf for wrong address', async () => {
        let result = await neo.get('balanceOf', [neo.sc.ContractParam.byteArray(neo.config.myAddress.replace('A','S'), 'address')]);
        expect(result[0].value).toEqual('');
    });
    it('should return 04 totalSupply', async () => {
        let result = await neo.get('totalSupply', [addressAsByteArray]);
        expect(result[0].value).toEqual('04');
    });


    describe("tokens of owner", function () {
        let tokenIds;
        before(async () => {
            let tokensOfOwnerResult = await neo.get('tokensOfOwner', [addressAsByteArray]);
            tokenIds = _.map(tokensOfOwnerResult[0].value, 'value');
        });
        it('4 tokens minted', async () => {
            expect(tokenIds.length).toEqual(4);
        });
        it('owner of tokens is me', async () => {
            await Aigle.forEach(tokenIds, async (tokenId) => {
                let tokensOfOwnerResult = await neo.get('ownerOf', [tokenId]);
                expect(tokensOfOwnerResult[0].value).toEqual(addressAsByteArray.value);
            });
        });

        describe('Lend', () => {
            before(async () => {
                if(hasMinted) {
                    return;
                }
               // await neo.call('lend', [addressAsByteArray, otherAddress, '02']);
            });

            it('should not have active Lend on fresh token', async () => {
                let result = await neo.get('isLendActive', ['01']);
                expect(result[0].value).toEqual('');
            });

            it('should have Lend active on Lent token', async () => {
                let result = await neo.get('isLendActive', ['02']);
                expect(result[0].value).toEqual('1');
            });

            it('owner of lend token is other address', async () => {
                let result = await neo.get('ownerOf', ['02']);
                expect(result[0].value).toEqual(otherAddress.value);
            });
            it('balanceOf other address is 01', async () => {
                let result = await neo.get('balanceOf', [otherAddress.value]);
                expect(result[0].value).toEqual('01');
            });
        });

        describe('Lend short period and return', () => {
            before(async () => {
                if(hasMinted) {
                    return;
                }
                // await neo.call('lend', [addressAsByteArray, otherAddress, '04', -1]);
                // await neo.call('returnToOwner', ['04']);
            });

            it('should not have active Lend on fresh token', async () => {
                let result = await neo.get('isLendActive', ['01']);
                expect(result[0].value).toEqual('');
            });

            it('should still have lend on long token', async () => {
                let result = await neo.get('isLendActive', ['02']);
                expect(result[0].value).toEqual('1');
            });

            it('should not have active Lend on returned token', async () => {
                let result = await neo.get('isLendActive', ['04']);
                expect(result[0].value).toEqual('');
            });

            it('returned token should have the ownerOf the original owner', async () => {
                let result = await neo.get('ownerOf', ['04']);
                expect(result[0].value).toEqual(addressAsByteArray.value);
            });
        });
    });
});
